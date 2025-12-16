using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IGTSAuthRepository
    {
        Task<BaseResponseDTO<ProviderAuthResponseDTO>> GetGTSAuthorizationToken();
    }
}
