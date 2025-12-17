using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public interface IMTSMaxStakeRepository
    {
        Task<BaseResponseDTO<MTSStakeResponseDTO>> GetMTSMaxStake(string authToken, MTSConfigRequestDTO request, decimal companyShare, bool isBetBuilder, decimal calculatedOdds);
    }
}
