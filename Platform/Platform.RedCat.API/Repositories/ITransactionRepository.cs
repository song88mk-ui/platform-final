using Platform.RedCat.API.Models;
using System.Threading.Tasks;
using WalletBalanceResponseDTO = Platform.RedCat.API.Models.WalletBalanceResponseDTO;

namespace Platform.RedCat.API.Repositories
{
    public interface ITransactionRepository
    {
        Task<RedCatBaseResponseDTO<ValidateTokenResponseDTO>> ValidateToken(ValidateTokenRequestDTO request);
        Task<RedCatBaseResponseDTO<WalletBalanceResponseDTO>> GetWalletBalance(WalletBalanceRequestDTO request, bool isInternalCall = false);
        Task<RedCatBaseResponseDTO<ApprovalResponseDTO>> GetApproval(ApprovalRequestDTO request, bool isInternalCall = false);
    }
}
