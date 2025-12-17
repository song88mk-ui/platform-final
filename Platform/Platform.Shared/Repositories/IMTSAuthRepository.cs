using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public interface IMTSAuthRepository
    {
        Task<BaseResponseDTO<MTSAuthResponseDTO>> GetMTSAuthorizationToken();
    }
}
