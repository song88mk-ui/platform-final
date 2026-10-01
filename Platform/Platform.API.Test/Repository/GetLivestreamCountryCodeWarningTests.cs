using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.API.Models;
using Platform.API.Repository;
using Platform.API.Telemetry;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;

namespace Platform.API.Test.Repository
{
    // Covers the GM-858 warning in PlatformRepository.GetLivestream (Platform.API/Repository/
    // PlatformRepository.cs, ~line 1336). The country code comes from x-country-code or
    // CF-IPCountry (BaseController.GetClientCountry). When neither header reaches platform-api,
    // the provider's geo check fails and the stream is reported as not available with no error
    // logged. On 2026-10-01 every livestream request served from sb.jmj22222.com and
    // sb.jmj19876.com arrived this way. These tests pin that a blank country is logged as a
    // warning carrying the client and fixture, that a real country is not, and that the warning
    // does not change what is sent to sportsbook-api.
    public class GetLivestreamCountryCodeWarningTests
    {
        private const string ExpectedTemplate =
            "GetLivestream: empty country code for clientId={ClientId}, providerFixtureId={ProviderFixtureId}; x-country-code and CF-IPCountry are both missing";

        private sealed record LogEntry(LogLevel Level, IReadOnlyDictionary<string, object?> State, string Message);

        private sealed class CapturingLogger : ILogger<PlatformRepository>
        {
            public List<LogEntry> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var values = state is IEnumerable<KeyValuePair<string, object?>> pairs
                    ? pairs.ToDictionary(p => p.Key, p => p.Value)
                    : new Dictionary<string, object?>();
                Entries.Add(new LogEntry(logLevel, values, formatter(state, exception)));
            }
        }

        private sealed class StubOperatorRepository : IOperatorRepository
        {
            private readonly StatusCode _status;

            public StubOperatorRepository(StatusCode status)
            {
                _status = status;
            }

            public Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent, OGClientRequest request) =>
                Task.FromResult(new BaseResponseDTO<AuthenticationResponseDTO>
                {
                    StatusCode = _status,
                    Result = new AuthenticationResponseDTO { UserId = Guid.NewGuid().ToString() }
                });

            public Task<BaseResponseDTO<LoginResponseDTO>> Login(string userName, string password, string endpoint, Dictionary<string, string>? headers = null) =>
                throw new NotImplementedException();

            public Task<BaseResponseDTO<object>> Logout(string operatorToken, string endpoint) =>
                throw new NotImplementedException();

            public Task<BaseResponseDTO<OperatorBetResponseDTO>> ResendCancellation(string endpoint, string info) =>
                throw new NotImplementedException();
        }

        // Only GetLivestream is reachable from PlatformRepository.GetLivestream; every other
        // member of this interface is untouched by it.
        private sealed class RecordingSportsbookRepository : ISportsbookRepository
        {
            public LivestreamRequestDTO? LastRequest { get; private set; }

            public Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(LivestreamRequestDTO request)
            {
                LastRequest = request;
                return Task.FromResult(new BaseResponseDTO<LivestreamResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new LivestreamResponseDTO()
                });
            }

            public Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(TopLeaguesRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(TopLeaguesRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues2(TopLeaguesRequest2DTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TopFixturesResponseDTO>> GetTopFixtures(TopFixturesRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TopFixturesIdsResponseDTO>> GetTopFixtureIds(TWRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups() => throw new NotImplementedException();
            public Task<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>> GetSelectorMarketTemplates(SelectorMarketTemplatesRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetFixtureResponseDTO>> GetBetFixture(BetFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetFixtureTranslationResponseDTO>> GetBetFixtureTranslation(BetFixtureTranslationRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetUserFavorites2(UserFavoriteRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(UserFavoriteRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddUserFavorites(UserFavoriteInsertDeleteRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteUserFavorites(UserFavoriteInsertDeleteRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures2(TWRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetActiveFixtures(ActiveFixturesRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>> GetActiveFixtureDetails(ActiveFixtureDetailsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(TWFLiveFixtureCountRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(TWFTodayFixtureCountRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(TWLiveFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixturesByLeague(TWLeagueLiveFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(TWTodayFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixturesByLeague(TWLeagueTodayFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixtures(TWUpcomingFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixturesByLeague(TWLeagueUpcomingFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(TWDailyFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixturesByLeague(TWLeagueDailyFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(TWTopLeagueFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetSegmentFixtures(TWSegmentFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(TWLeagueFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<SportsResponseDTO>> GetSports(SportsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(TWFixtureDetailsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetSelectionDetails(TWFixtureMarketSelectionDetailsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(TWOutrightFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixturesByLeague(TWLeagueOutrightFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(TWSearchRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures(ClientTopFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures2(ClientTopFixtureRequestDTO2 request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BackofficeSportsResponseDTO>> GetSportstreeDetails(BackofficeSportsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> PostClientTopFixtures(PostClientTopFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> CopyClientTopFixtures(CopyClientTopFixtureRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ClientTopLeaguesResponseDTO>> GetClientTopLeagues(ClientTopLeaguesRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> AddOrUpdateTopLeagues(ClientTopLeagueDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> CopyClientTopLeagues(CopyClientTopLeagueRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<SportsLookupResponseDTO>> GetClientSportsLookup(SportsLookupRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<SportGroupResponseDTO>> GetSportGroups() => throw new NotImplementedException();
            public Task<BaseResponseDTO<BoostedResponseDTO>> GetBoostedFixtures(BoostedFixturesRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetBuilderMarketTemplatesDTO>> GetBetBuilderMarketTemplates() => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>> GetBetBuilderSelectionsOdds(BetBuilderSelectionsOddsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>> GetMarketSortOrder(MarketSortOrderRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>> GetFixtureSortOrder(FixtureSortOrderRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<List<MarketTemplatePlayerPropsResponseDTO>>> GetMarketTemplatePlayerProps(MarketTemplatePlayerPropsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<List<TranslationResponseDTO>>> GetTranslationByLanguageCode(TranslationRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<List<LeagueHighlightsResponseDTO>>> GetLeagueHighlights(LeagueHighlightsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<TWResponseDTO>> GetLeagueHighlightsFixtures(LeagueHighlightsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<bool>> TranslationRecovery(TranslationRecoveryRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<AnnouncementResponseDTO>> GetAnnouncements(AnnouncementRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BackofficeAnnouncementResponseDTO>> GetAnnouncements(BackofficeAnnouncementRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BackofficeAnnouncementResponseDTO>> AddOrUpdateAnnouncements(AddOrUpdateBackofficeAnnouncementRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<bool>> DeleteClientAnnouncements(DeleteBackofficeAnnouncementDTO request) => throw new NotImplementedException();
        }

        // GetClient checks this cache first; returning null forces it through to the seeded
        // PlatformContext.
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

        private static (PlatformRepository Repository, RecordingSportsbookRepository Sportsbook, CapturingLogger Logger) CreateRepository(
            Guid clientGuid, StatusCode authenticationStatus = StatusCode.Success)
        {
            var context = InMemoryPlatformContext.Create();
            context.Client.Add(new Client
            {
                ClientId = clientGuid,
                ClientCode = "CODE",
                ClientName = "Test Client",
                IsActive = true,
                Endpoint = "http://localhost",
                EffectiveDate = DateTime.UtcNow.AddYears(-1),
                LimitId = "L1",
                ClientIdRef = 1,
                IsAgent = false
            });
            context.SaveChanges();

            var sportsbook = new RecordingSportsbookRepository();
            var logger = new CapturingLogger();

            var repository = new PlatformRepository(
                operatorRepository: new StubOperatorRepository(authenticationStatus),
                bettingRepository: null!,
                sportsbookRepository: sportsbook,
                casinoRepository: null!,
                webPropsRepository: null!,
                platformContext: context,
                memCache: new NoOpMemCache(),
                configuration: new ConfigurationBuilder().Build(),
                messageBroker: null!,
                instrumentation: new Instrumentation(),
                logger: logger);

            return (repository, sportsbook, logger);
        }

        private static LivestreamRequest ARequest(string providerFixtureId = "14525392") => new LivestreamRequest
        {
            ProviderFixtureId = providerFixtureId,
            IPAddress = "203.0.113.10",
            IsMobile = false
        };

        private static List<LogEntry> Warnings(CapturingLogger logger) =>
            logger.Entries.Where(e => e.Level == LogLevel.Warning).ToList();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetLivestream_BlankCountryCode_LogsOneWarningWithClientAndFixture(string? countryCode)
        {
            var clientGuid = Guid.NewGuid();
            var (repository, _, logger) = CreateRepository(clientGuid);

            await repository.GetLivestream("api-key", clientGuid.ToString(), "operator-token", countryCode!, ARequest("14525392"), new OGClientRequest());

            var warning = Assert.Single(Warnings(logger));
            Assert.Equal(ExpectedTemplate, warning.State["{OriginalFormat}"]);
            Assert.Equal(clientGuid.ToString(), warning.State["ClientId"]);
            Assert.Equal("14525392", warning.State["ProviderFixtureId"]);
        }

        [Theory]
        [InlineData("HK")]
        [InlineData("MU")]
        public async Task GetLivestream_CountryCodePresent_LogsNoWarning(string countryCode)
        {
            var clientGuid = Guid.NewGuid();
            var (repository, _, logger) = CreateRepository(clientGuid);

            await repository.GetLivestream("api-key", clientGuid.ToString(), "operator-token", countryCode, ARequest(), new OGClientRequest());

            Assert.Empty(Warnings(logger));
        }

        [Theory]
        [InlineData("")]
        [InlineData("HK")]
        public async Task GetLivestream_StillCallsSportsbookWithUnchangedRequest(string countryCode)
        {
            var clientGuid = Guid.NewGuid();
            var (repository, sportsbook, _) = CreateRepository(clientGuid);

            var response = await repository.GetLivestream("api-key", clientGuid.ToString(), "operator-token", countryCode, ARequest("14525392"), new OGClientRequest());

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.NotNull(sportsbook.LastRequest);
            Assert.Equal(countryCode, sportsbook.LastRequest!.IPCountryCode);
            Assert.Equal("14525392", sportsbook.LastRequest.ProviderFixtureId);
            Assert.Equal("203.0.113.10", sportsbook.LastRequest.IPAddress);
        }

        [Fact]
        public async Task GetLivestream_AuthenticationFails_LogsNoWarningAndSkipsSportsbook()
        {
            var clientGuid = Guid.NewGuid();
            var (repository, sportsbook, logger) = CreateRepository(clientGuid, StatusCode.Error);

            var response = await repository.GetLivestream("api-key", clientGuid.ToString(), "operator-token", "", ARequest(), new OGClientRequest());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Null(sportsbook.LastRequest);
            Assert.Empty(Warnings(logger));
        }
    }
}
