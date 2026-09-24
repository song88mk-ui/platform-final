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

        private OperatorSettlementRetry StoredRetry(Guid betId)
        {
            using var context = _factory.CreateDbContext();
            return context.OperatorSettlementRetry.Find(betId);
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

            repository.ProcessBetPartialCashOut(message, _broker, Guid.NewGuid().ToString(), ThreadId);

            // Job.Resettlement deserializes the row as the base OnBetSettlement before republishing,
            // so the replayed message carries only the base fields. The repository must reload the
            // partial amounts from the retry row.
            wallet.Status = HttpStatusCode.OK;
            wallet.Body = "{\"statusId\":1000,\"balance\":540}";
            var replay = new OnBetSettlementPartial { Bet = message.Bet, IsRetry = true };

            repository.ProcessBetPartialCashOut(replay, _broker, Guid.NewGuid().ToString(), ThreadId);

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
    }
}
