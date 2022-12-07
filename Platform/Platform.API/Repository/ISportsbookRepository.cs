using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(LivestreamRequestDTO request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(TopLeaguesRequestDTO request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(TopLeaguesRequestDTO request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues2(TopLeaguesRequest2DTO request);
        Task<BaseResponseDTO<TopFixturesResponseDTO>> GetTopFixtures(TopFixturesRequestDTO request);
        Task<BaseResponseDTO<TopFixturesIdsResponseDTO>> GetTopFixtureIds(TWRequestDTO request);
        Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups();
        Task<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>> GetSelectorMarketTemplates(SelectorMarketTemplatesRequestDTO request);
        Task<BaseResponseDTO<BetFixtureResponseDTO>> GetBetFixture(BetFixtureRequestDTO request);
        Task<BaseResponseDTO<BetFixtureTranslationResponseDTO>> GetBetFixtureTranslation(BetFixtureTranslationRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetUserFavorites2(UserFavoriteRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(UserFavoriteRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddUserFavorites(UserFavoriteInsertDeleteRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteUserFavorites(UserFavoriteInsertDeleteRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures2(TWRequestDTO request);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(TWFLiveFixtureCountRequestDTO request);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(TWFTodayFixtureCountRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(TWLiveFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixturesByLeague(TWLeagueLiveFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(TWTodayFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixturesByLeague(TWLeagueTodayFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixtures(TWUpcomingFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(TWDailyFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixturesByLeague(TWLeagueDailyFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(TWTopLeagueFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetSegmentFixtures(TWSegmentFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(TWLeagueFixtureRequestDTO request);
        Task<BaseResponseDTO<SportsResponseDTO>> GetSports(SportsRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(TWFixtureDetailsRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetSelectionDetails(TWFixtureMarketSelectionDetailsRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(TWOutrightFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixturesByLeague(TWLeagueOutrightFixtureRequestDTO request);
        Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(TWSearchRequestDTO request);
        Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures(ClientTopFixtureRequestDTO request);
        Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures2(ClientTopFixtureRequestDTO2 request);
        Task<BaseResponseDTO<BackofficeSportsResponseDTO>> GetSportstreeDetails(BackofficeSportsRequestDTO request);
        Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> PostClientTopFixtures(PostClientTopFixtureRequestDTO request);
        Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> CopyClientTopFixtures(CopyClientTopFixtureRequestDTO request);
        Task<BaseResponseDTO<ClientTopLeaguesResponseDTO>> GetClientTopLeagues(ClientTopLeaguesRequestDTO request);
        Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> AddOrUpdateTopLeagues(ClientTopLeagueDTO request);
        Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> CopyClientTopLeagues(CopyClientTopLeagueRequestDTO request);
        Task<BaseResponseDTO<SportsLookupResponseDTO>> GetClientSportsLookup(SportsLookupRequestDTO request);
        Task<BaseResponseDTO<SportGroupResponseDTO>> GetSportGroups();
        Task<BaseResponseDTO<BoostedResponseDTO>> GetBoostedFixtures(BoostedFixturesRequestDTO request);
        Task<BaseResponseDTO<BetBuilderMarketTemplatesDTO>> GetBetBuilderMarketTemplates();
        Task<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>> GetBetBuilderSelectionsOdds(BetBuilderSelectionsOddsRequestDTO request);
        Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>> GetMarketSortOrder(MarketSortOrderRequestDTO request);
        Task<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>> GetFixtureSortOrder(FixtureSortOrderRequestDTO request);
        Task<BaseResponseDTO<List<MarketTemplatePlayerPropsResponseDTO>>> GetMarketTemplatePlayerProps(MarketTemplatePlayerPropsRequestDTO request);

    }
}
