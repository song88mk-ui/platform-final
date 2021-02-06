using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IBettingRepository
    {
        Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request);
    }
}
