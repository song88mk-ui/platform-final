using Platform.Shared.Models;
using Platform.Shared.Models.Provider.BetGenius;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public interface IBetGeniusRepository
    {
        Task<BaseResponseDTO<BGAuth>> GetBetGeniusAuthorizationToken();
        Task<BaseResponseDTO<BGEvent>> GetBetGeniusFixture(string token, string fixtureId);
    }
}
