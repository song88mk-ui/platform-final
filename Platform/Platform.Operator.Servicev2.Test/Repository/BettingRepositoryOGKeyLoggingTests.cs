using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Operator.Servicev2.Repository;
using Platform.Operator.Servicev2.Telemetry;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using System.Net;

namespace Platform.Operator.Servicev2.Test.Repository
{
    // GM-880: ProcesssBetPlacementOG logged the OG private API key in plain text on all three
    // failure paths, and PRD logs ship to Loki. Each call site now logs Mask(). Stryker cannot
    // guard this: Platform.Shared.Test's stryker-config ignores Log* calls, and no mutator removes
    // an extension call from an argument. These tests drive each failure path and fail if any log
    // line carries the raw key, so reverting one Mask() call breaks its test.
    //
    // CancelBet builds a SQL Server BettingContext inline with no connection string, so it throws
    // after each failure path logs. That throw reaches the catch-all, which logs again, and then
    // escapes the method. The tests tolerate it and assert only on what was logged.
    public class BettingRepositoryOGKeyLoggingTests
    {
        private const string PrivateKey = "og-private-key-0123456789abcdef";
        private static readonly string MaskedKey = new string('*', PrivateKey.Length - 4) + "cdef";
        private const string ThreadId = "1";

        private readonly InMemoryPlatformContextFactory _factory = new InMemoryPlatformContextFactory();
        private readonly FakeMessageBroker _broker = new FakeMessageBroker();
        private readonly CapturingLogger _logger = new CapturingLogger();

        private BettingRepository CreateRepository() => new BettingRepository(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Custom:OrientalGaming:PublicAPIKey"] = "og-public-key",
                    ["Custom:OrientalGaming:PrivateAPIKey"] = PrivateKey,
                    ["Custom:OrientalGaming:VendorName"] = "song88",
                    ["Custom:OrientalGaming:HighCurrenciesAsString"] = "VND"
                })
                .Build(),
            _factory,
            new Instrumentation(),
            _logger);

        private static OnBetPlacement OGBetPlacement(string endpoint) => new OnBetPlacement
        {
            Bet = new Bet
            {
                BetId = Guid.NewGuid(),
                UserId = "user-1",
                ClientId = "256D6197-22A4-412E-852D-9A6611493618",
                ClientCode = "OG",
                Endpoint = endpoint,
                OperatorToken = "operator-token|USD|SPORTS",
                CurrencyCode = "USD",
                OddsTypeName = "Decimal",
                Odds = 2.5m,
                Stake = 50m,
                TrueStake = 50m
            },
            IsOGClient = true,
            BetSelections = new List<BetSelectionMessageExtenstion>
            {
                new BetSelectionMessageExtenstion
                {
                    BetSelection = new BetSelection
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
                        Odds = 2.5m
                    },
                    MarketSelectionCount = 3
                }
            }
        };

        private void PlaceBet(string endpoint)
        {
            // CancelBet throws on every failure path here; see the class comment.
            Record.Exception(() =>
                CreateRepository().ProcesssBetPlacementOG(OGBetPlacement(endpoint), _broker, Guid.NewGuid().ToString(), ThreadId));
        }

        private CapturingLogger.Entry Logged(string templatePrefix) =>
            Assert.Single(_logger.Entries, e => e.Template.StartsWith(templatePrefix));

        private void AssertNoLogLineHasTheRawKey()
        {
            Assert.NotEmpty(_logger.Entries);
            Assert.All(_logger.Entries, e => Assert.DoesNotContain(PrivateKey, e.Message));
        }

        [Fact]
        public void BetPlacementOG_OperatorRejectsDebit_LogsTheMaskedKey()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"E-000\",\"rs_message\":\"rejected\"}");

            PlaceBet(og.Endpoint);

            Assert.Equal(MaskedKey, Logged("BetPlacement OG Failed").Arguments["Key"]);
            AssertNoLogLineHasTheRawKey();
        }

        [Fact]
        public void BetPlacementOG_OperatorReturnsErrorStatus_LogsTheMaskedKey()
        {
            using var og = new OperatorStubServer(HttpStatusCode.InternalServerError, "");

            PlaceBet(og.Endpoint);

            Assert.Equal(MaskedKey, Logged("BetPlacement OG GetClient Failed").Arguments["Key"]);
            AssertNoLogLineHasTheRawKey();
        }

        [Fact]
        public void BetPlacementOG_OperatorUnreachable_LogsTheMaskedKey()
        {
            PlaceBet(OperatorStubServer.UnreachableEndpoint());

            Assert.Equal(MaskedKey, Logged("Error BetPLacement OG").Arguments["@privateKey"]);
            AssertNoLogLineHasTheRawKey();
        }

        // Records each log call's template, its named arguments and the rendered message, so a test
        // can check both the value passed for {Key} and that no rendered line contains the secret.
        private sealed class CapturingLogger : ILogger<BettingRepository>
        {
            internal sealed record Entry(string Template, IReadOnlyDictionary<string, object> Arguments, string Message);

            private readonly List<Entry> _entries = new List<Entry>();

            internal IReadOnlyList<Entry> Entries
            {
                get { lock (_entries) return _entries.ToList(); }
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var values = state as IEnumerable<KeyValuePair<string, object>> ?? Enumerable.Empty<KeyValuePair<string, object>>();
                var arguments = values.ToDictionary(v => v.Key, v => v.Value);
                var template = arguments.TryGetValue("{OriginalFormat}", out var format) ? format as string : string.Empty;

                lock (_entries)
                    _entries.Add(new Entry(template, arguments, formatter(state, exception)));
            }
        }
    }
}
