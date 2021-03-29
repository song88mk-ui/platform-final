using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IMTSMaxStakeRepository
    {
        Task<BaseResponseDTO<MTSStakeResponseDTO>> GetMTSMaxStake(string authToken, MTSConfigRequestDTO request);
    }
}
