using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Operator.Servicev2.Repository;
using Platform.Operator.Servicev2.Telemetry;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using System.Net;
using System.Text.Json;

namespace Platform.Operator.Servicev2.Test.Repository
{
    // GM-879: the two CancelBet call sites in ProcesssBetPlacementOG, the rejected-debit branch and
    // the catch block, must cancel the bet locally and then send OG a rollback built from the
    // operator token's OG parameters. These tests seed the bet in the in-memory betting database,
    // place it against OperatorStubServer, and check both halves. Before the BettingContext seam,
    // CancelBet could not run in a test, and Stryker left every mutant on both call sites alive.
    public class BettingRepositoryOGPlacementCancelTests
    {
        private const string ThreadId = "1";
        private const string DebitPath = "/api/v2/platform/transaction/bulk/debit";

        private readonly InMemoryPlatformContextFactory _factory = new InMemoryPlatformContextFactory();
        private readonly InMemoryBettingContextFactory _bettingFactory = new InMemoryBettingContextFactory();
        private readonly FakeMessageBroker _broker = new FakeMessageBroker();

        private BettingRepository CreateRepository() => new BettingRepository(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Custom:OrientalGaming:PublicAPIKey"] = "test-public-key",
                ["Custom:OrientalGaming:PrivateAPIKey"] = "test-private-key",
                ["Custom:OrientalGaming:VendorName"] = "test-vendor",
                ["Custom:OrientalGaming:HighCurrenciesAsString"] = "VND"
            }).Build(),
            _factory,
            _bettingFactory,
            new Instrumentation(),
            NullLogger<BettingRepository>.Instance);

        // The operator token carries VND while the bet's own CurrencyCode is USD, so a rollback in
        // VND proves the call site passed the token's OG parameters through.
        private OnBetPlacement SeededPlacement(string endpoint)
        {
            var bet = new Bet
            {
                BetId = Guid.NewGuid(),
                UserId = "user-1",
                ClientId = "0a1b2c3d-0000-0000-0000-000000000001",
                ClientCode = "T01",
                Endpoint = endpoint,
                OperatorToken = "operator-token|VND|SPORTS",
                CurrencyCode = "USD",
                OddsTypeName = "Decimal",
                Odds = 2.5m,
                Stake = 50000m,
                TrueStake = 50000m,
                IsFreeBet = true,
                BetStatusId = (int)BetStatus.Running,
                BetStatusName = BetStatus.Running.ToString()
            };
            var selection = new BetSelection
            {
                BetSelectionId = Guid.NewGuid(),
                BetId = bet.BetId,
                UserId = bet.UserId,
                SportId = 1,
                SportName = "Soccer",
                LeagueName = "Premier League",
                FixtureId = 1001,
                FixtureName = "Home FC vs. Away FC",
                FixtureTypeId = (int)FixtureType.Match,
                FixtureStartDate = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc),
                MarketId = 2001,
                MarketName = "Match Result",
                SelectionId = 3001,
                SelectionName = "Home FC",
                Odds = 2.5m,
                BetSelectionStatusId = (int)BetStatus.Running,
                BetSelectionStatusName = BetStatus.Running.ToString()
            };

            using (var context = _bettingFactory.CreateDbContext())
            {
                context.Bet.Add(bet);
                context.BetSelection.Add(selection);
                context.SaveChanges();
            }

            return new OnBetPlacement
            {
                Bet = bet,
                IsOGClient = true,
                BetSelections = new List<BetSelectionMessageExtenstion>
                {
                    new BetSelectionMessageExtenstion { BetSelection = selection, MarketSelectionCount = 3 }
                }
            };
        }

        private Exception Place(OnBetPlacement message) => Record.Exception(() =>
            CreateRepository().ProcesssBetPlacementOG(message, _broker, Guid.NewGuid().ToString(), ThreadId));

        private Bet StoredBet(Guid betId)
        {
            using var context = _bettingFactory.CreateDbContext();
            return context.Bet.Find(betId);
        }

        private void AssertCancelledLocally(Guid betId)
        {
            var bet = StoredBet(betId);
            Assert.Equal((int)BetStatus.Cancelled, bet.BetStatusId);
            Assert.Equal("103 - Operator Error", bet.Reason);
            Assert.False(bet.IsFreeBet);

            using var context = _bettingFactory.CreateDbContext();
            Assert.Contains(context.BetAudit, a => a.BetId == betId && a.BetStatusId == (int)BetStatus.Cancelled);
        }

        // The debit and the rollback both go to the OG bulk debit path. The non-OG cancellation
        // would go to /v1/bet/cancellation instead.
        private static JsonElement Rollback(OperatorStubServer og)
        {
            Assert.DoesNotContain(og.Requests, r => r.Path == "/v1/bet/cancellation");
            var rollback = Assert.Single(
                og.Requests.Where(r => r.Path == DebitPath).Select(r => JsonDocument.Parse(r.Body).RootElement),
                b => b.GetProperty("transaction_type").GetString() == "rollback");
            Assert.Equal("VND", rollback.GetProperty("currency").GetString());
            Assert.Equal("SPORTS", rollback.GetProperty("game_code").GetString());
            Assert.Equal(50m, rollback.GetProperty("total_amount").GetDecimal());
            return rollback;
        }

        [Fact]
        public void OgRejectsDebit_CancelsLocallyAndSendsTheRollback()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"E-103\",\"rs_message\":\"rejected\"}");
            var message = SeededPlacement(og.Endpoint);

            var exception = Place(message);

            Assert.Null(exception);
            AssertCancelledLocally(message.Bet.BetId);
            Rollback(og);
            Assert.Equal(2, og.Requests.Count);
        }

        // Pins current behaviour around GM-892: OG answers every request with invalid JSON, so the
        // rollback's own response handling also fails and the exception escapes. What matters here
        // is that the catch block cancelled the bet and sent the rollback with the OG parameters.
        // The escaping exception is asserted so this test fails, and is updated, when GM-892 lands.
        [Fact]
        public void DebitResponseCannotBeRead_CancelsLocallyAndSendsTheRollback()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "not json");
            var message = SeededPlacement(og.Endpoint);

            var exception = Place(message);

            Assert.NotNull(exception);

            AssertCancelledLocally(message.Bet.BetId);
            Rollback(og);
        }
    }
}
