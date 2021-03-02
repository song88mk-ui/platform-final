using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Repository
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
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddFavorites(UserFavoriteInsertDeleteRequestDTO request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteFavorites(UserFavoriteInsertDeleteRequestDTO request);

        Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetActiveFixtures(ActiveFixturesRequestDTO request);
        Task<BaseResponseDTO<ActiveFixtureCountResponseDTO>> GetActiveFixtureCount(ActiveFixtureCountRequestDTO request);
        Task<BaseResponseDTO<SportsLookupResponseDTO>> GetSportsLookup();
        Task<BaseResponseDTO<SegmentsLookupResponseDTO>> GetSegmentsLookup();
        Task<BaseResponseDTO<LeaguesLookupResponseDTO>> GetLeaguesLookup();
    }
}
