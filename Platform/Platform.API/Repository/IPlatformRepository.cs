using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IPlatformRepository
    {
        Task<BaseResponseDTO<WalletBalanceResponseDTO>> GetWalletBalance(string clientId, string operatorToken);
        Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(string apiKey, string clientId, string operatorToken, BetRequest request);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string operatorToken, BetHistoryRequest request);
        Task<BaseResponseDTO<BetConfigFEResponseDTO>> GetBetConfig(string apiKey, string clientId, BetConfigRequest request);
        Task<BaseResponseDTO<ClientResponseDTO>> AddOrUpdateClient(ClientRequestDTO request);
        Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(string apiKey, string clientId, string operatorToken, LivestreamRequest request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(string apiKey, string clientId, TopLeaguesRequest request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(string apiKey, string clientId, TopLeaguesRequest request);
        Task<BaseResponseDTO<TopFixturesResponseDTO>> GetTopFixtures(string apiKey, string clientId, TopFixturesRequest request);
        Task<BaseResponseDTO<TopFixturesIdsResponseDTO>> GetTopFixtureIds(string apiKey, string clientId, TopFixturesRequest request);
        Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups(string apiKey, string clientId);
        Task<BaseResponseDTO<TWResponseDTO>> GetUserFavorites2(string apiKey, string clientId, string operatorToken);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(string apiKey, string clientId, string operatorToken);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddUserFavorites(string apiKey, string clientId, string operatorToken, InsertDeleteUserFavoritesRequest request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteUserFavorites(string apiKey, string clientId, string operatorToken, InsertDeleteUserFavoritesRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures2(string apiKey, string clientId, TopFixturesRequest request);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(string apiKey, string clientId, FixtureCountRequest request);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(string apiKey, string clientId, TodayFixtureCountRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(string apiKey, string clientId, FixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(string apiKey, string clientId, TodayFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(string apiKey, string clientId, DailyFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(string apiKey, string clientId, TopLeagueFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetSegmentFixtures(string apiKey, string clientId, SegmentFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(string apiKey, string clientId, LeagueFixtureRequest request);
        Task<BaseResponseDTO<SportsResponseDTO>> GetSports(string apiKey, string clientId, SportRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(string apiKey, string clientId, FixtureDetailsRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetSelectionDetails(string apiKey, string clientId, FixtureMarketSelectionDetailsRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(string apiKey, string clientId, OutrightFixtureRequest request);
        Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(string apiKey, string clientId, SearchResultRequest request);
        Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures(string clientRefIds);
        Task<BaseResponseDTO<BackofficeSportsResponseDTO>> GetSportstreeDetails(int clientRefId);
        Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> PostClientTopFixtures(ClientTopFixtureRequest request);
        Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> AddOrUpdateTopLeagues(BackOfficeClientTopLeague request);
        Task<BaseResponseDTO<BackOfficeTopLeaguesResponseDTO>> GetClientTopLeagues(BackOfficeTopLeaguesRequestDTO request);
        Task<BaseResponseDTO<SportsLookupResponseDTO>> GetSportsLookup(BackofficeClientSportsRequestDTO request);
    }
}
