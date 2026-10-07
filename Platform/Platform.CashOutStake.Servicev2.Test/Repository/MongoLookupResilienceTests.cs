using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.CashOutStake.Servicev2.Repository;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using System.Diagnostics;

namespace Platform.CashOutStake.Servicev2.Test.Repository
{
    // GM-901: platform-final/Platform/Platform.CashOutStake.Servicev2 fell back to an unreachable
    // preprod Mongo default. Each failed market/selection lookup blocked the RabbitMQ consumer
    // for the driver's 30s default server-selection timeout, and ProcessPartialCashOut mutated
    // the in-memory stake before the lookup, so a failure meant the updated cashout offer was
    // never published. These tests point the repository at an address nothing listens on, with a
    // short serverSelectionTimeoutMS so the failure path runs in well under a second.
    public class MongoLookupResilienceTests
    {
        private const string UnreachableMongoConnectionString =
            "mongodb://127.0.0.1:1/sportsbook?connectTimeoutMS=200&serverSelectionTimeoutMS=400";

        private sealed class NoOpMemCache : IMemCache
        {
            public void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout) { }
            public void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date) { }
            public T? GetCache<T>(string key, Func<T> repositoryCall, int expiration) where T : class => null;
            public T? GetCache<T>(string key) where T : class => null;
            public KeyValuePair<bool, long> GetCache(string key) => default;
            public void DeleteCache(string key) { }
            public void DeleteCacheByPrefix(string prefix) { }
        }

        private sealed class RecordingMessageBroker : IMessageBroker
        {
            public List<object> PublishedPayloads { get; } = new List<object>();

#pragma warning disable CS0067 // never raised by this fake; required by the interface
            public event EventHandler<BasicDeliverEventArgs>? MessageReceived;
#pragma warning restore CS0067
            public void Connect() { }
            public void ExternalConnect() { }
            public void ConnectWithAutoAck() { }
            public void ConnectWithManualAck() { }
            public void Acknowledge(BasicDeliverEventArgs e) { }
            public void NAcknowledge(BasicDeliverEventArgs e) { }
            public void Publish<T>(T payload, string messageType, string routingKey, string correlationId) => PublishedPayloads.Add(payload!);
            public void Publish<T>(T payload, string messageType, string routingKey, string correlationId, int priority) => PublishedPayloads.Add(payload!);
            public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId) => PublishedPayloads.Add(payload!);
            public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId, int priority) => PublishedPayloads.Add(payload!);
            public void Publish(string payload, string messageType, string routingKey, string correlationId) { }
            public void CPublish(string payload, string messageType, string routingKey, string correlationId) { }
            public void ExternalPublish<T>(T payload, string messageType, string routingKey, string correlationId) { }
            public void Dispose() { }
        }

        private static BettingRepository CreateRepository(string mongoConnectionString)
        {
            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:MongoDB-Sportsbook"] = mongoConnectionString
                })
                .Build();

            return new BettingRepository(serviceProvider, configuration, new NoOpMemCache(), NullLogger<BettingRepository>.Instance);
        }

        private static (BetMemory bet, OnBetSettlementPartial message) APartialCashoutFor(Guid betId)
        {
            var bet = new BetMemory
            {
                BetId = betId,
                ClientCode = "CODE",
                ClientId = Guid.NewGuid().ToString(),
                UserId = Guid.NewGuid().ToString(),
                TrueStake = 100m,
                Stake = 100m,
                OddsTypeId = (int)OddsType.Decimal
            };

            bet.BetSelections.Add(new BetSelectionMemory
            {
                BetId = betId,
                BetSelectionId = Guid.NewGuid(),
                MarketId = 1,
                SelectionId = 1,
                BetSelectionStatusId = (int)BetStatus.Running,
                Odds = 2.0m,
                DeadHeatFactor = 1.0m,
                IsBinary = true
            });

            var message = new OnBetSettlementPartial
            {
                Bet = new Bet { BetId = betId },
                CashoutStake = 40m,
                CashoutStakePercent = 0.4m,
                Stake = 40m,
                TrueStake = 40m
            };

            return (bet, message);
        }

        [Fact]
        public void Constructor_MissingMongoConnectionString_ThrowsInvalidOperationException()
        {
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var configuration = new ConfigurationBuilder().Build();

            Assert.Throws<InvalidOperationException>(() =>
                new BettingRepository(serviceProvider, configuration, new NoOpMemCache(), NullLogger<BettingRepository>.Instance));
        }

        [Fact]
        public void ProcessPartialCashOut_MongoLookupFails_StillPublishesCashOutChange()
        {
            var repository = CreateRepository(UnreachableMongoConnectionString);
            var betId = Guid.NewGuid();
            var (bet, message) = APartialCashoutFor(betId);
            repository._bets.Add(bet);

            var broker = new RecordingMessageBroker();

            var exception = Record.Exception(() => repository.ProcessPartialCashOut(message, broker));

            Assert.Null(exception);

            var payload = Assert.Single(broker.PublishedPayloads);
            var cashOutChange = Assert.IsType<OnCashOutChange>(payload);
            // Market/selection never landed in cache because Mongo was unreachable, so
            // GetCashOut's existing null-check branch correctly reports Unavailable rather than
            // publishing a stale or fabricated offer.
            Assert.Equal(CashOutStatus.Unavailable, cashOutChange.CashOutStatus);
        }

        [Fact]
        public void ProcessPartialCashOut_RepeatedCallsAfterMongoFailure_SkipsRetryDuringBackoffWindow()
        {
            var repository = CreateRepository(UnreachableMongoConnectionString);
            var betId = Guid.NewGuid();
            var (bet, message) = APartialCashoutFor(betId);
            repository._bets.Add(bet);

            var broker = new RecordingMessageBroker();

            var firstCall = Stopwatch.StartNew();
            repository.ProcessPartialCashOut(message, broker);
            firstCall.Stop();

            var secondCall = Stopwatch.StartNew();
            repository.ProcessPartialCashOut(message, broker);
            secondCall.Stop();

            Assert.Equal(2, broker.PublishedPayloads.Count);
            Assert.True(firstCall.Elapsed.TotalMilliseconds >= 100,
                $"Expected the first call to actually attempt the Mongo connection (took {firstCall.Elapsed}).");
            Assert.True(secondCall.Elapsed.TotalMilliseconds < firstCall.Elapsed.TotalMilliseconds / 2,
                $"Expected the second call to skip Mongo via the failure backoff (first {firstCall.Elapsed}, second {secondCall.Elapsed}).");
        }
    }
}
