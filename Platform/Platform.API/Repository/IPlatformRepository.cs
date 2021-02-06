using Platform.API.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IPlatformRepository
    {
        Task<BaseResponse<WalletBalanceResponse>> GetWalletBalance(string clientId, string operatorToken);
        Task<BaseResponse<BetResponse>> PlaceBet(string clientId, string operatorToken, BetRequest request);
        Task<BaseResponse<BetHistoryResponse>> GetBetHistory(string clientId, string operatorToken, BetHistoryRequest request);
    }
}
