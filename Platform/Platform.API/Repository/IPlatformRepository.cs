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
        Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(string apiKey, string clientId, string operatorToken, BetRequest request);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string langCode, string operatorToken, BetHistoryRequest request);
        Task<BaseResponseDTO<BetConfigFEResponseDTO>> GetBetConfig(string apiKey, string clientId, BetConfigRequest request);
        Task<BaseResponseDTO<ClientResponseDTO>> AddOrUpdateClient(ClientRequestDTO request);
        Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(string apiKey, string clientId, string operatorToken, LivestreamRequest request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(string apiKey, string clientId, string langCode, TopLeaguesRequest request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(string apiKey, string clientId, string langCode, TopLeaguesRequest request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues2(string apiKey, string clientId, TopLeaguesRequest2 request);
        Task<BaseResponseDTO<TopFixturesIdsResponseDTO>> GetTopFixtureIds(string apiKey, string clientId, string langCode);
        Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups(string apiKey, string clientId);
        Task<BaseResponseDTO<TWResponseDTO>> GetUserFavorites2(string apiKey, string clientId, string langCode, string operatorToken);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(string apiKey, string clientId, string langCode, string operatorToken);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddUserFavorites(string apiKey, string clientId, string langCode, string operatorToken, InsertDeleteUserFavoritesRequest request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteUserFavorites(string apiKey, string clientId, string langCode, string operatorToken, InsertDeleteUserFavoritesRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures(string apiKey, string clientId, string langCode);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(string apiKey, string clientId, string langCode);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(string apiKey, string clientId, string langCode, TodayFixtureCountRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(string apiKey, string clientId, string langCode, FixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixturesByLeague(string apiKey, string clientId, string langCode, FixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(string apiKey, string clientId, string langCode, TodayFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixturesByLeague(string apiKey, string clientId, string langCode, TodayFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(string apiKey, string clientId, string langCode, DailyFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixturesByLeague(string apiKey, string clientId, string langCode, DailyFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(string apiKey, string clientId, string langCode, TopLeagueFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetSegmentFixtures(string apiKey, string clientId, string langCode, SegmentFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(string apiKey, string clientId, string langCode, LeagueFixtureRequest request);
        Task<BaseResponseDTO<SportsResponseDTO>> GetSports(string apiKey, string clientId, string langCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(string apiKey, string clientId, string langCode, FixtureDetailsRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetSelectionDetails(string apiKey, string clientId, string langCode, FixtureMarketSelectionDetailsRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(string apiKey, string clientId, string langCode, OutrightFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixturesByLeague(string apiKey, string clientId, string langCode, OutrightFixtureRequest request);
        Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(string apiKey, string clientId, string langCode, SearchResultRequest request);
        Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures(string clientRefIds);
        Task<BaseResponseDTO<BackofficeSportsResponseDTO>> GetSportstreeDetails(int clientRefId);
        Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> PostClientTopFixtures(ClientTopFixtureRequest request);
        Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> AddOrUpdateTopLeagues(BackOfficeClientTopLeague request);
        Task<BaseResponseDTO<BackOfficeTopLeaguesResponseDTO>> GetClientTopLeagues(BackOfficeTopLeaguesRequestDTO request);
        Task<BaseResponseDTO<SportsLookupResponseDTO>> GetClientSportsLookup(BackofficeClientSportsRequestDTO request);
        Task<BaseResponseDTO<BetCashOutResponseDTO>> CashoutBet(string apiKey, string clientId, string operatorToken, CashoutBetRequest request);

    }
}
