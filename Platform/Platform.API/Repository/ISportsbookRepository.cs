using Platform.API.Models;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(LivestreamRequestDTO request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(TopLeaguesRequestDTO request);
        Task<BaseResponseDTO<TopFixturesResponseDTO>> GetTopFixtures(TopFixturesRequestDTO request);
        Task<BaseResponseDTO<OrderedSportsResponseDTO>> GetOrderedSports(OrderedSportsRequestDTO request);
        Task<BaseResponseDTO<OrderedLeaguesResponseDTO>> GetOrderedLeagues(OrderedLeaguesRequestDTO request);
        Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups();
        Task<BaseResponseDTO<BetFixtureResponseDTO>> GetBetFixture(BetFixtureRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(UserFavoriteRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddUserFavorites(UserFavoriteInsertDeleteRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteUserFavorites(UserFavoriteInsertDeleteRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures2(TWRequestDTO request);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(TWFLiveFixtureCountRequestDTO request);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(TWFTodayFixtureCountRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(TWLiveFixtureRequestDTO request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(TWTodayFixtureRequestDTO request);
    }
}
