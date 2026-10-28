using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Operator.Servicev2.Repository;
using Platform.Operator.Servicev2.Telemetry;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using System.Net;

namespace Platform.Operator.Servicev2.Test.Repository
{
    // ProcesssBetCancellation (non-OG) publishes one back-office cancellation on r.bet.cancellation
    // before it calls the operator, and every failure branch publishes a second one. Provider.Service
    // switches on "OnBetBackOfficeCancellation", so a message with any other type is dropped
    // silently. These tests pin the type on the success path and on each of the four failure paths.
    public class BettingRepositoryCancellationTests
    {
        private const string ThreadId = "1";

        private readonly InMemoryPlatformContextFactory _factory = new InMemoryPlatformContextFactory();
        private readonly InMemoryBettingContextFactory _bettingFactory = new InMemoryBettingContextFactory();
        private readonly FakeMessageBroker _broker = new FakeMessageBroker();

        private BettingRepository CreateRepository() => new BettingRepository(
            new ConfigurationBuilder().Build(),
            _factory,
            _bettingFactory,
            new Instrumentation(),
            NullLogger<BettingRepository>.Instance);

        private static OnBetSettlement Cancellation(string endpoint) => new OnBetSettlement
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
                Stake = 50m,
                TrueStake = 50m,
                Odds = 2.5m,
                BetStatusId = (int)BetStatus.Cancelled,
                BetStatusName = BetStatus.Cancelled.ToString()
            },
            BetSelections = new List<BetSelection>
            {
                new BetSelection
                {
                    BetSelectionId = Guid.NewGuid(),
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
                    BetSelectionStatusName = BetStatus.Cancelled.ToString(),
                    Odds = 2.5m
                }
            },
            Message = "Operator Error"
        };

        private List<PublishedMessage> BackOfficeCancellations() =>
            _broker.Published.Where(p => p.RoutingKey == "r.bet.cancellation").ToList();

        private OperatorSettlementRetry StoredRetry(Guid betId)
        {
            using var context = _factory.CreateDbContext();
            return context.OperatorSettlementRetry.SingleOrDefault(r => r.BetId == betId);
        }

        // One row per failure branch: an operator error code on 200, an error code on 400, a server
        // error, and a 200 body that cannot be parsed, which lands in the catch block.
        public static IEnumerable<object[]> FailedCancellations => new List<object[]>
        {
            new object[] { HttpStatusCode.OK, "{\"statusId\":1003}" },
            new object[] { HttpStatusCode.BadRequest, "{\"statusId\":1003}" },
            new object[] { HttpStatusCode.InternalServerError, "" },
            new object[] { HttpStatusCode.OK, "not json" }
        };

        [Theory]
        [MemberData(nameof(FailedCancellations))]
        public void OperatorRejectsCancellation_NotifiesProviderServiceTwiceAndStoresARetry(HttpStatusCode status, string body)
        {
            using var wallet = new OperatorStubServer(status, body);
            var message = Cancellation(wallet.Endpoint);

            CreateRepository().ProcesssBetCancellation(message, _broker, "corr-1", ThreadId);

            var backOffice = BackOfficeCancellations();
            Assert.Equal(2, backOffice.Count);
            Assert.All(backOffice, p => Assert.Equal("OnBetBackOfficeCancellation", p.MessageType));
            Assert.All(backOffice, p => Assert.Equal(message.Bet.BetId, Assert.IsType<OnBetBackofficeCancellation>(p.Payload).BetId));

            var retry = StoredRetry(message.Bet.BetId);
            Assert.NotNull(retry);
            Assert.Equal("OnBetCancellation", retry.MessageType);
            Assert.Equal("corr-1", retry.CorrelationId);
            Assert.False(retry.IsSuccess);
            Assert.DoesNotContain(_broker.Published, p => p.MessageType == "OnWalletChange");
        }

        [Fact]
        public void OperatorAcceptsCancellation_NotifiesProviderServiceOnce()
        {
            using var wallet = new OperatorStubServer(HttpStatusCode.OK, "{\"statusId\":1000,\"balance\":540}");
            var message = Cancellation(wallet.Endpoint);

            CreateRepository().ProcesssBetCancellation(message, _broker, "corr-1", ThreadId);

            var published = Assert.Single(BackOfficeCancellations());
            Assert.Equal("OnBetBackOfficeCancellation", published.MessageType);
            Assert.Equal(message.Bet.BetId, Assert.IsType<OnBetBackofficeCancellation>(published.Payload).BetId);
            Assert.Contains(_broker.Published, p => p.MessageType == "OnWalletChange");
            Assert.Null(StoredRetry(message.Bet.BetId));
        }
    }
}
