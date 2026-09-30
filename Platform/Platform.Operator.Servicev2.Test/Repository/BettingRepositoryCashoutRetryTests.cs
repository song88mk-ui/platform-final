using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Operator.Servicev2.Repository;
using Platform.Operator.Servicev2.Telemetry;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.MessageModels;
using System.Net;
using System.Text.Json;

namespace Platform.Operator.Servicev2.Test.Repository
{
    // GM-862: when the operator wallet call for a partial cashout failed, the retry row was stored
    // as OnBetFullCashOut on two of the four failure branches, and on every branch it lost the
    // partial-only fields. Job.Resettlement replays the row under its stored MessageType, so the
    // player could be credited the full remaining payout, or a partial with a zero payout.
    public class BettingRepositoryCashoutRetryTests
    {
        private const string PartialPath = "/v1/bet/partial/cashout";
        private const string ThreadId = "1";

        private readonly InMemoryPlatformContextFactory _factory = new InMemoryPlatformContextFactory();
        private readonly FakeMessageBroker _broker = new FakeMessageBroker();

        private BettingRepository CreateRepository() => new BettingRepository(
            new ConfigurationBuilder().Build(),
            _factory,
            new Instrumentation(),
            NullLogger<BettingRepository>.Instance);

        private static OnBetSettlementPartial PartialCashout(string endpoint) => new OnBetSettlementPartial
        {
            Bet = new Bet
            {
                BetId = Guid.NewGuid(),
                UserId = "user-1",
                ClientId = "0A1B2C3D-0000-0000-0000-000000000001",
                ClientCode = "T01",
                Endpoint = endpoint,
                OperatorToken = "operator-token|USD",
                CurrencyCode = "USD",
                BetStatusName = "Running",
                Odds = 2.5m,
                Payout = 125m,
                SettlementDate = new DateTime(2026, 9, 24, 8, 0, 0, DateTimeKind.Utc)
            },
            CashoutStake = 40m,
            CashoutStakePercent = 0.5m,
            Stake = 50m,
            TrueStake = 50m,
            TaxedStake = 1.5m,
            NonTaxedPayout = 40m
        };

        private static OnBetSettlement FullCashout(string endpoint) => new OnBetSettlement
        {
            Bet = PartialCashout(endpoint).Bet
        };

        // GM-862: OperatorSettlementRetry is keyed by (BetId, CorrelationId). These tests each
        // drive a single attempt per bet, so a plain BetId filter still identifies one row; the
        // GM-862 composite-key tests below assert the multi-row behavior directly.
        private OperatorSettlementRetry StoredRetry(Guid betId)
        {
            using var context = _factory.CreateDbContext();
            return context.OperatorSettlementRetry.SingleOrDefault(r => r.BetId == betId);
        }

        public static IEnumerable<object[]> FailedWalletResponses => new List<object[]>
        {
            new object[] { HttpStatusCode.InternalServerError, "" },
            new object[] { HttpStatusCode.BadRequest, "{\"statusId\":1003}" },
            new object[] { HttpStatusCode.OK, "{\"statusId\":1003}" }
        };

        [Theory]
        [MemberData(nameof(FailedWalletResponses))]
        public void PartialCashout_WalletCallFails_StoresRetryAsPartialCashout(HttpStatusCode status, string body)
        {
            using var wallet = new OperatorStubServer(status, body);
            var message = PartialCashout(wallet.Endpoint);

            CreateRepository().ProcessBetPartialCashOut(message, _broker, Guid.NewGuid().ToString(), ThreadId);

            var retry = StoredRetry(message.Bet.BetId);
            Assert.NotNull(retry);
            Assert.Equal("OnBetPartialCashOut", retry.MessageType);
            Assert.True(retry.IsRetry);
        }

        [Fact]
        public void PartialCashout_WalletUnreachable_StoresRetryAsPartialCashout()
        {
            var message = PartialCashout(OperatorStubServer.UnreachableEndpoint());

            CreateRepository().ProcessBetPartialCashOut(message, _broker, Guid.NewGuid().ToString(), ThreadId);

            var retry = StoredRetry(message.Bet.BetId);
            Assert.NotNull(retry);
            Assert.Equal("OnBetPartialCashOut", retry.MessageType);
        }

        [Fact]
        public void PartialCashout_WalletCallFails_RetryRowKeepsThePartialFields()
        {
            using var wallet = new OperatorStubServer(HttpStatusCode.InternalServerError, "");
            var message = PartialCashout(wallet.Endpoint);

            CreateRepository().ProcessBetPartialCashOut(message, _broker, Guid.NewGuid().ToString(), ThreadId);

            var stored = JsonSerializer.Deserialize<OnBetSettlementPartial>(StoredRetry(message.Bet.BetId).Info);
            Assert.Equal(40m, stored.CashoutStake);
            Assert.Equal(0.5m, stored.CashoutStakePercent);
            Assert.Equal(50m, stored.Stake);
            Assert.Equal(50m, stored.TrueStake);
            Assert.Equal(1.5m, stored.TaxedStake);
        }

        [Fact]
        public void PartialCashout_ReplayedRetry_PaysThePartialAmount()
        {
            using var wallet = new OperatorStubServer(HttpStatusCode.InternalServerError, "");
            var message = PartialCashout(wallet.Endpoint);
            var repository = CreateRepository();
            var correlationId = Guid.NewGuid().ToString();

            repository.ProcessBetPartialCashOut(message, _broker, correlationId, ThreadId);

            // Job.Resettlement deserializes the row as the base OnBetSettlement before republishing,
            // so the replayed message carries only the base fields. The repository must reload the
            // partial amounts from the retry row. It also republishes under the row's own
            // CorrelationId (GM-862: the row's key), which a real replay always carries unchanged.
            wallet.Status = HttpStatusCode.OK;
            wallet.Body = "{\"statusId\":1000,\"balance\":540}";
            var replay = new OnBetSettlementPartial { Bet = message.Bet, IsRetry = true };

            repository.ProcessBetPartialCashOut(replay, _broker, correlationId, ThreadId);

            var replayed = JsonDocument.Parse(wallet.Requests.Last().Body).RootElement;
            Assert.Equal(PartialPath, wallet.Requests.Last().Path);
            Assert.Equal(40m, replayed.GetProperty("payout").GetDecimal());
            Assert.Equal(50m, replayed.GetProperty("stake").GetDecimal());
            Assert.Equal(1.5m, replayed.GetProperty("taxedStake").GetDecimal());

            var retry = StoredRetry(message.Bet.BetId);
            Assert.True(retry.IsSuccess);
            Assert.False(retry.IsRetry);
            Assert.Contains(_broker.Published, p => p.MessageType == "OnWalletChange");
        }

        [Fact]
        public void PartialCashout_RetryWithoutStoredRow_ReturnsWithoutCallingTheWallet()
        {
            using var wallet = new OperatorStubServer(HttpStatusCode.OK, "{\"statusId\":1000}");
            var replay = PartialCashout(wallet.Endpoint);
            replay.IsRetry = true;

            CreateRepository().ProcessBetPartialCashOut(replay, _broker, Guid.NewGuid().ToString(), ThreadId);

            Assert.Empty(wallet.Requests);
        }

        [Fact]
        public void FullCashout_RetryWithoutStoredRow_ReturnsWithoutCallingTheWallet()
        {
            using var wallet = new OperatorStubServer(HttpStatusCode.OK, "{\"statusId\":1000}");
            var replay = FullCashout(wallet.Endpoint);
            replay.IsRetry = true;

            CreateRepository().ProcessBetFullCashOut(replay, _broker, Guid.NewGuid().ToString(), ThreadId);

            Assert.Empty(wallet.Requests);
        }

        [Fact]
        public void FullCashout_WalletCallFails_StoresRetryAsFullCashout()
        {
            using var wallet = new OperatorStubServer(HttpStatusCode.InternalServerError, "");
            var message = FullCashout(wallet.Endpoint);

            CreateRepository().ProcessBetFullCashOut(message, _broker, Guid.NewGuid().ToString(), ThreadId);

            Assert.Equal("OnBetFullCashOut", StoredRetry(message.Bet.BetId).MessageType);
        }

        // GM-862: OperatorSettlementRetry used to be keyed by BetId alone, so a second independent
        // failure on the same bet silently overwrote an earlier still-pending retry. The key is now
        // (BetId, CorrelationId); these two tests cover the overwrite directly.
        [Fact]
        public void PartialCashout_TwoIndependentFailuresOnSameBet_BothRetriesPersist()
        {
            using var wallet = new OperatorStubServer(HttpStatusCode.InternalServerError, "");
            var repository = CreateRepository();
            var betId = Guid.NewGuid();

            var first = PartialCashout(wallet.Endpoint);
            first.Bet.BetId = betId;
            first.CashoutStake = 30m;
            repository.ProcessBetPartialCashOut(first, _broker, Guid.NewGuid().ToString(), ThreadId);

            var second = PartialCashout(wallet.Endpoint);
            second.Bet.BetId = betId;
            second.CashoutStake = 20m;
            repository.ProcessBetPartialCashOut(second, _broker, Guid.NewGuid().ToString(), ThreadId);

            using var context = _factory.CreateDbContext();
            var rows = context.OperatorSettlementRetry.Where(r => r.BetId == betId).ToList();

            Assert.Equal(2, rows.Count);
            var stakes = rows
                .Select(r => JsonSerializer.Deserialize<OnBetSettlementPartial>(r.Info).CashoutStake)
                .OrderBy(s => s)
                .ToList();
            Assert.Equal(new[] { 20m, 30m }, stakes);
        }

        [Fact]
        public void PartialCashout_SuccessOnOneAttempt_LeavesAnIndependentPendingAttemptOnTheSameBetAlone()
        {
            using var wallet = new OperatorStubServer(HttpStatusCode.InternalServerError, "");
            var repository = CreateRepository();
            var betId = Guid.NewGuid();
            var firstCorrelationId = Guid.NewGuid().ToString();

            var first = PartialCashout(wallet.Endpoint);
            first.Bet.BetId = betId;
            first.CashoutStake = 30m;
            repository.ProcessBetPartialCashOut(first, _broker, firstCorrelationId, ThreadId);

            // A second, independent, first-try-successful partial cashout on the same bet never
            // creates a row of its own (the table only tracks failures) - the point of this test is
            // that it must not reach into and resolve the first attempt's still-pending row either,
            // which is exactly what the old BetId-only key let happen.
            wallet.Status = HttpStatusCode.OK;
            wallet.Body = "{\"statusId\":1000,\"balance\":520}";

            var second = PartialCashout(wallet.Endpoint);
            second.Bet.BetId = betId;
            second.CashoutStake = 20m;
            repository.ProcessBetPartialCashOut(second, _broker, Guid.NewGuid().ToString(), ThreadId);

            using var context = _factory.CreateDbContext();
            var rows = context.OperatorSettlementRetry.Where(r => r.BetId == betId).ToList();

            var stillPending = Assert.Single(rows);
            Assert.Equal(firstCorrelationId, stillPending.CorrelationId);
            Assert.True(stillPending.IsRetry);
            Assert.False(stillPending.IsSuccess);
            Assert.Equal(30m, JsonSerializer.Deserialize<OnBetSettlementPartial>(stillPending.Info).CashoutStake);
        }
    }
}
