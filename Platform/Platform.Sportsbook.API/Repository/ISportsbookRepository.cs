using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(LivestreamRequestDTO request);
        Task<BaseResponseDTO<BetSettlementScoreResponseDTO>> GetBetSettlementScore(BetSettlementScoreRequestDTO request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(TopLeaguesRequestDTO request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(TopLeaguesRequestDTO request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues2(TopLeaguesRequest2DTO request);
        Task<BaseResponseDTO<TopFixturesResponseDTO>> GetTopFixtures(TopFixturesRequestDTO request);
        Task<BaseResponseDTO<TopFixturesIdsResponseDTO>> GetTopFixtureIds(TopFixturesRequestDTO request);
        Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups();
        //Task<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>> GetSelectorMarketTemplates();
        Task<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>> GetClientSelectorMarketTemplates(SelectorMarketTemplatesRequestDTO request);
        Task<BaseResponseDTO<BetFixtureResponseDTO>> GetBetFixture(BetFixtureRequestDTO request);
        Task<BaseResponseDTO<BetFixtureTranslationResponseDTO>> GetBetFixtureTranslation(BetFixtureTranslationRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(UserFavoriteRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetUserFavoritesAll(UserFavoriteRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddFavorites(UserFavoriteInsertDeleteRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteFavorites(UserFavoriteInsertDeleteRequestDTO request);

        Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetActiveFixtures(ActiveFixturesRequestDTO request);
        Task<BaseResponseDTO<ActiveFixtureCountResponseDTO>> GetActiveFixtureCount(ActiveFixtureCountRequestDTO request);
        Task<BaseResponseDTO<SportsLookupResponseDTO>> GetSportsLookup(SportsLookupRequestDTO request);
        Task<BaseResponseDTO<SegmentsLookupResponseDTO>> GetSegmentsLookup(SegmentsLookupRequestDTO request);
        Task<BaseResponseDTO<LeaguesLookupResponseDTO>> GetLeaguesLookup(LeaguesLookupRequestDTO request);

        Task<BaseResponseDTO<TWResponseDTO>> GetTopFixturesTW(TWRequestDTO request);        
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(TWFLiveFixtureCountRequestDTO request);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(TWFTodayFixtureCountRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(TWLiveFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixturesByLeague(TWLeagueLiveFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(TWTodayFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixturesByLeague(TWLeagueTodayFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixtures(TWUpcomingFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixturesByLeague(TWLeagueUpcomingFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(TWDailyFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixturesByLeague(TWLeagueDailyFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(TWTopLeagueFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(TWSegmentFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(TWLeagueFixtureRequestDTO request);

        Task<BaseResponseDTO<BetSelectionProbabilityResponseDTO>> GetBetSelectionProbabilities(BetSelectionProbabilityRequestDTO request);

        Task<BaseResponseDTO<SportsResponseDTO>> GetSports(SportsRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(TWFixtureDetailsRequestDTO request);
        Task<BaseResponseDTO<TWLimitedResponseDTO>> GetFixtureDetailsLimited(TWFixtureDetailsRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtureMarketSelectionDetails(TWFixtureMarketSelectionDetailsRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(TWOutrightFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixturesByLeague(TWLeagueOutrightFixtureRequestDTO request);
        Task<BaseResponseDTO<StatusCode>> ManualSettlement(BetSettlementRequestDTO request);

        Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(TWSearchRequestDTO request);
        Task<BaseResponseDTO<StatusCode>> ManualCancellation(BetCancellationRequestDTO request);

        Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures(ClientTopFixtureRequestDTO request);
        Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures2(ClientTopFixtureRequestDTO2 request);
        Task<BaseResponseDTO<BackofficeSportsResponseDTO>> GetSportstreeDetails(BackofficeSportsRequestDTO request);
        Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> PostClientTopFixtures(PostClientTopFixtureRequestDTO request);        
        Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> CopyClientTopFixtures(CopyClientTopFixtureRequestDTO request);        

        Task<BaseResponseDTO<ClientTopLeaguesResponseDTO>> GetClientIdsTopLeagues(ClientTopLeaguesRequestDTO request);
        Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> AddOrUpdateTopLeagues(ClientTopLeagueDTO request);
        Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> CopyClientTopLeagues(CopyClientTopLeagueRequestDTO request);
        Task<BaseResponseDTO<SportsLookupResponseDTO>> GetClientSportsLookup(string clientCode);
        Task<BaseResponseDTO<SportGroupResponseDTO>> GetSportGroups();
        Task<BaseResponseDTO<BoostedResponseDTO>> GetBoostedFixtures(BoostedFixturesRequestDTO request);

        Task<BaseResponseDTO<BetBuilderMarketTemplatesDTO>> GetBetBuilderMarketTemplatesDTO();
        Task<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>> GetBetBuilderSelectionsOdds(BetBuilderSelectionsOddsRequestDTO request);
        Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO>> GetClientMarketTemplateGroups(ClientMarketTemplateGroupRequestDTO request);
        Task<BaseResponseDTO<ClientMarketTemplateResponseDTO>> GetClientMarketTemplates(ClientMarketTemplateRequestDTO request);
        Task<BaseResponseDTO<ClientMarketTemplateResponseDTO>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequestDTO request);
        Task<BaseResponseDTO<ClientAddMarketTemplateGroupResponseDTO>> AddClientMarketTemplateGroup(ClientAddMarketTemplateGroupRequestDTO request);
        Task<BaseResponseDTO<ClientPostMarketTemplateGroupResponseDTO>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequestDTO request);
        Task<BaseResponseDTO<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequestDTO request);
        Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequestDTO request);
        Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>> GetClientMarketSortOrder(MarketSortOrderRequestDTO request);
        Task<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>> GetClientFixtureSortOrder(FixtureSortOrderRequestDTO request);
        Task<BaseResponseDTO<List<MarketTemplatePlayerPropsResponseDTO>>> GetMarketTemplatePlayerProps(MarketTemplatePlayerPropsRequestDTO request);
    }
}
