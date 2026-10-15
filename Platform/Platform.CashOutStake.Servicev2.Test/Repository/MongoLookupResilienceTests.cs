using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.CashOutStake.Servicev2.Repository;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;

namespace Platform.CashOutStake.Servicev2.Test.Repository
{
    // GM-901: platform-final/Platform/Platform.CashOutStake.Servicev2 fell back to an unreachable
    // preprod Mongo default. Each failed market/selection lookup blocked the RabbitMQ consumer
    // for the driver's 30s default server-selection timeout, and ProcessPartialCashOut mutated
    // the in-memory stake before the lookup, so a failure meant the updated cashout offer was
    // never published. One test points the real repository at an address nothing listens on,
    // with a short serverSelectionTimeoutMS so the failure path runs in well under a second. The
    // rest override the FindMarket/FindSelection seam, so they count lookups instead of timing
    // them and can return "not found" without a live Mongo.
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

        internal sealed class RecordingMessageBroker : IMessageBroker
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

        private sealed class CapturingLogger : ILogger<BettingRepository>
        {
            public List<(LogLevel Level, string Message)> Entries { get; } = new List<(LogLevel, string)>();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => Entries.Add((logLevel, formatter(state, exception)));
        }

        // Replaces the two Mongo queries with delegates and counts how often each one runs.
        internal sealed class FakeLookupBettingRepository : BettingRepository
        {
            private readonly Func<long, CashOutMarketFilter?> _findMarket;
            private readonly Func<long, CashOutSelectionFilter?> _findSelection;

            public int MarketLookups { get; private set; }
            public int SelectionLookups { get; private set; }

            public FakeLookupBettingRepository(IMemCache memCache, ILogger<BettingRepository> logger,
                Func<long, CashOutMarketFilter?> findMarket, Func<long, CashOutSelectionFilter?> findSelection)
                : base(new ServiceCollection().BuildServiceProvider(), MongoConfiguration(UnreachableMongoConnectionString), memCache, logger)
            {
                _findMarket = findMarket;
                _findSelection = findSelection;
            }

            protected override CashOutMarketFilter? FindMarket(long marketId)
            {
                MarketLookups++;
                return _findMarket(marketId);
            }

            protected override CashOutSelectionFilter? FindSelection(long selectionId)
            {
                SelectionLookups++;
                return _findSelection(selectionId);
            }
        }

        private static IConfiguration MongoConfiguration(string mongoConnectionString) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:MongoDB-Sportsbook"] = mongoConnectionString
                })
                .Build();

        private static BettingRepository CreateRepository(string mongoConnectionString) =>
            new BettingRepository(new ServiceCollection().BuildServiceProvider(), MongoConfiguration(mongoConnectionString),
                new NoOpMemCache(), NullLogger<BettingRepository>.Instance);

        // The real MemCache wraps the process-wide MemoryCache.Default, so tests that use it take
        // fresh ids to stay independent of each other.
        private static long _nextId = DateTime.UtcNow.Ticks;
        private static long NewId() => Interlocked.Increment(ref _nextId);

        private static BetMemory ARunningBet(long fixtureId, long marketId, long selectionId)
        {
            var betId = Guid.NewGuid();

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
                FixtureId = fixtureId,
                MarketId = marketId,
                SelectionId = selectionId,
                BetSelectionStatusId = (int)BetStatus.Running,
                Odds = 2.0m,
                DeadHeatFactor = 1.0m,
                IsBinary = true
            });

            return bet;
        }

        private static OnBetSettlementPartial APartialCashoutFor(Guid betId) => new OnBetSettlementPartial
        {
            Bet = new Bet { BetId = betId },
            CashoutStake = 40m,
            CashoutStakePercent = 0.4m,
            Stake = 40m,
            TrueStake = 40m
        };

        private static void Track(BettingRepository repository, BetMemory bet)
        {
            repository._bets.Add(bet);
            repository._fixtureBetReference.Add(new Tuple<long, List<Guid>>(bet.BetSelections[0].FixtureId, new List<Guid> { bet.BetId }));
        }

        private static OnExternalOddsChangeMessage AnOddsChangeFor(long fixtureId) => new OnExternalOddsChangeMessage
        {
            FixtureId = fixtureId,
            FixtureStatusId = 2,
            IsSuspended = false
        };

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
            var bet = ARunningBet(fixtureId: 1, marketId: 1, selectionId: 1);
            repository._bets.Add(bet);

            var broker = new RecordingMessageBroker();

            var exception = Record.Exception(() => repository.ProcessPartialCashOut(APartialCashoutFor(bet.BetId), broker));

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
            var repository = new FakeLookupBettingRepository(new NoOpMemCache(), NullLogger<BettingRepository>.Instance,
                _ => throw new TimeoutException("Mongo unreachable"),
                _ => throw new TimeoutException("Mongo unreachable"));
            var bet = ARunningBet(fixtureId: 1, marketId: 1, selectionId: 1);
            repository._bets.Add(bet);

            var broker = new RecordingMessageBroker();

            repository.ProcessPartialCashOut(APartialCashoutFor(bet.BetId), broker);
            repository.ProcessPartialCashOut(APartialCashoutFor(bet.BetId), broker);

            Assert.Equal(2, broker.PublishedPayloads.Count);
            Assert.Equal(1, repository.MarketLookups);
            Assert.Equal(1, repository.SelectionLookups);
        }

        [Fact]
        public void ProcessPartialCashOut_MarketAndSelectionNotInMongo_LogsWarningNotErrorAndPublishesUnavailable()
        {
            // Real MemCache: MemoryCache.Set throws on a null value, which the old code hit when
            // Mongo returned no document and then logged as a Mongo failure.
            var logger = new CapturingLogger();
            var repository = new FakeLookupBettingRepository(new MemCache(), logger, _ => null, _ => null);
            var bet = ARunningBet(NewId(), NewId(), NewId());
            repository._bets.Add(bet);

            var broker = new RecordingMessageBroker();

            var exception = Record.Exception(() => repository.ProcessPartialCashOut(APartialCashoutFor(bet.BetId), broker));

            Assert.Null(exception);
            var cashOutChange = Assert.IsType<OnCashOutChange>(Assert.Single(broker.PublishedPayloads));
            Assert.Equal(CashOutStatus.Unavailable, cashOutChange.CashOutStatus);

            Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Market") && e.Message.Contains("not found"));
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Selection") && e.Message.Contains("not found"));
        }

        [Fact]
        public void ProcessPartialCashOut_RepeatedCallsAfterNotFound_SkipsRetryDuringBackoffWindow()
        {
            var repository = new FakeLookupBettingRepository(new MemCache(), NullLogger<BettingRepository>.Instance, _ => null, _ => null);
            var bet = ARunningBet(NewId(), NewId(), NewId());
            repository._bets.Add(bet);

            var broker = new RecordingMessageBroker();

            repository.ProcessPartialCashOut(APartialCashoutFor(bet.BetId), broker);
            repository.ProcessPartialCashOut(APartialCashoutFor(bet.BetId), broker);

            Assert.Equal(2, broker.PublishedPayloads.Count);
            Assert.Equal(1, repository.MarketLookups);
            Assert.Equal(1, repository.SelectionLookups);
        }

        [Fact]
        public void ProcessOddsChange_MongoLookupFails_StillPublishesUnavailableCashOutChange()
        {
            var repository = new FakeLookupBettingRepository(new NoOpMemCache(), NullLogger<BettingRepository>.Instance,
                _ => throw new TimeoutException("Mongo unreachable"),
                _ => throw new TimeoutException("Mongo unreachable"));
            var bet = ARunningBet(fixtureId: 7, marketId: 1, selectionId: 1);
            Track(repository, bet);

            var broker = new RecordingMessageBroker();

            var exception = Record.Exception(() => repository.ProcessOddsChange(AnOddsChangeFor(7), broker));

            Assert.Null(exception);
            var cashOutChange = Assert.IsType<OnCashOutChange>(Assert.Single(broker.PublishedPayloads));
            Assert.Equal(bet.BetId.ToString().ToUpper(), cashOutChange.BetId);
            Assert.Equal(CashOutStatus.Unavailable, cashOutChange.CashOutStatus);
        }

        [Fact]
        public void ProcessOddsChange_LookupSucceeds_CachesResultAndDoesNotQueryMongoAgain()
        {
            var fixtureId = NewId();
            var marketId = NewId();
            var selectionId = NewId();

            var repository = new FakeLookupBettingRepository(new MemCache(), NullLogger<BettingRepository>.Instance,
                id => new CashOutMarketFilter { MaId = id, MaStId = (int)MarketStatus.Active },
                id => new CashOutSelectionFilter { SeId = id, MaId = marketId, MaStId = (int)MarketStatus.Active, IsAc = true, Pr = 0.5, WoPr = 0.5, LoPr = 0.5 });
            Track(repository, ARunningBet(fixtureId, marketId, selectionId));

            var broker = new RecordingMessageBroker();

            repository.ProcessOddsChange(AnOddsChangeFor(fixtureId), broker);
            repository.ProcessOddsChange(AnOddsChangeFor(fixtureId), broker);

            Assert.Equal(2, broker.PublishedPayloads.Count);
            Assert.Equal(1, repository.MarketLookups);
            Assert.Equal(1, repository.SelectionLookups);
        }
    }
}
