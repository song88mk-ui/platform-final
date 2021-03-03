using Platform.API.Models;
using Platform.Shared.Models;
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
        Task<BaseResponseDTO<OrderedSportsResponseDTO>> GetOrderedSports(string apiKey, string clientId, OrderedSportsRequest request);
        Task<BaseResponseDTO<OrderedLeaguesResponseDTO>> GetOrderedLeagues(string apiKey, string clientId, OrderedLeaguesRequest request);
        Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups(string apiKey, string clientId);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(string apiKey, string clientId, string operatorToken);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddUserFavorites(string apiKey, string clientId, string operatorToken, InsertDeleteUserFavoritesRequest request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteUserFavorites(string apiKey, string clientId, string operatorToken, InsertDeleteUserFavoritesRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures2(string apiKey, string clientId, TopFixturesRequest request);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(string apiKey, string clientId, FixtureCountRequest request);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(string apiKey, string clientId, TodayFixtureCountRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(string apiKey, string clientId, FixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(string apiKey, string clientId, TodayFixtureRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(string apiKey, string clientId, LeagueFixtureRequest request);
    }
}
