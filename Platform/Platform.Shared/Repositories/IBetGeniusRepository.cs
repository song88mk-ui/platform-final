using Platform.Shared.Models.Provider.BetGenius;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public interface IBetGeniusRepository
    {
        Task<BaseResponseDTO<BGAuth>> GetBetGeniusAuthorizationToken();
        Task<BaseResponseDTO<BGEvent>> GetBetGeniusFixture(string token, string fixtureId);
        Task<BaseResponseDTO<BGLiveStreamResponseDto>> GetLiveStreamToken(string token, string fixtureId, string liveStreamId, string deliveryId, string region, string device);
    }
}
