using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public interface IGTSAuthRepository
    {
        Task<BaseResponseDTO<ProviderAuthResponseDTO>> GetGTSAuthorizationToken();
    }
}
