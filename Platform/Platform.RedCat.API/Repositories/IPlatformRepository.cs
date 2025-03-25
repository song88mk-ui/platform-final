using Platform.RedCat.API.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;
using WalletBalanceResponse = Platform.RedCat.API.Models.WalletBalanceResponse;

namespace Platform.RedCat.API.Repositories
{
    public interface IPlatformRepository
    {
        Task<RedCatBaseResponse<ValidateTokenResponse>> ValidateToken(ValidateTokenRequest request);
        Task<RedCatBaseResponse<WalletBalanceResponse>> GetWalletBalance(WalletBalanceRequest request, bool isInternalCall = false);
        Task<RedCatBaseResponse<ApprovalResponse>> GetApproval(ApprovalRequest request, bool isInternalCall = false);
        Task<RedCatBaseResponse<RedCatBetTransactionResponseDTO>> Stake(RedCatBetTransactionRequest request);
        Task<RedCatBaseResponse<RedCatBetTransactionResponseDTO>> Settle(RedCatBetTransactionRequest request);
        Task<RedCatBaseResponse<RedCatBetTransactionResponseDTO>> VoidStake(RedCatVoidBetTransactionRequest request);
        Task<RedCatBaseResponse<RedCatBetTransactionResponseDTO>> VoidSettle(RedCatVoidBetTransactionRequest request);
    }
}
