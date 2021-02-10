using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IPlatformRepository
    {
        Task<AuthenticationResponseDTO> Authenticate(string operatorToken);
        Task<OperatorBetResponseDTO> WalletBalance(string operatorToken);
        Task<OperatorBetResponseDTO> BetPlacement(OperatorBetRequestDTO request, string operatorToken);
        Task<OperatorBetResponseDTO> BetSettlement(OperatorBetSettlementRequestDTO request);
        Task<OperatorBetResponseDTO> BetCancellation(string userId, decimal stake);
    }
}
