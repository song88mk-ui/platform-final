using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public interface IGTSRepository
    {
        Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStake(string authToken, BetRequestDTO request);
    }
}
