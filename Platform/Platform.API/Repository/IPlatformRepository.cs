using Platform.API.Models;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IPlatformRepository
    {
        Task<BaseResponseDTO<WalletBalanceResponseDTO>> GetWalletBalance(string clientId, string operatorToken);
        Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(string apiKey, string clientId, string operatorToken, BetRequest request);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string clientId, string operatorToken, BetHistoryRequest request);
    }
}
