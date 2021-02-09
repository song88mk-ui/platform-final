using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IPlatformRepository
    {
        Task<AuthenticationResponseDTO> Authenticate(string operatorToken);
        Task<OperatorBetResponseDTO> WalletBalance(string operatorToken);
        Task<OperatorBetResponseDTO> BetPlacement(string operatorToken, decimal stake, string currencyCode);
        Task<OperatorBetResponseDTO> BetSettlement(string userId, decimal payout);
        Task<OperatorBetResponseDTO> BetCancellation(string userId, decimal stake);
    }
}
