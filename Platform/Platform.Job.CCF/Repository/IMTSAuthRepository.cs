using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Job.CCF.Repository
{
    public interface IMTSAuthRepository
    {
        Task<BaseResponseDTO<MTSAuthResponseDTO>> GetMTSAuthorizationToken();
    }
}
