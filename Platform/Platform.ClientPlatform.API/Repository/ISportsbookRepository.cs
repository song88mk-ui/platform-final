using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures(string clientId);
    }
}
