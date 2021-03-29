using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IMTSAuthRepository
    {
        Task<BaseResponseDTO<MTSAuthResponseDTO>> GetMTSAuthorizationToken();
    }
}
