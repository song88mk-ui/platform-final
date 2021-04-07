using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IBettingRepository
    {
        Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request);
        Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(BetRequestDTO request);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request);
        Task<BaseResponseDTO<BetConfigFEResponseDTO>> GetBetConfig(BetConfigRequestDTO request);
        Task<BaseResponseDTO<BetModifyResponseDTO>> CancelBets(BetModifyRequestDTO request);
    }
}
