using Platform.Operator.API.Models;
using Platform.Shared.Models;
using System.Threading.Tasks;
using BetPlacementResponseDTO = Platform.Operator.API.Models.BetPlacementResponseDTO;
using LoginResponseDTO = Platform.Operator.API.Models.LoginResponseDTO;
using LogoutResponseDTO = Platform.Operator.API.Models.LogoutResponseDTO;

namespace Platform.Operator.API.Repository
{
    public interface IOperatorRepository
    {
        Task<LoginResponseDTO> Login(LoginRequestDTO request, string userAgent = "", string ipAddress = "", string country = "");
        Task<LogoutResponseDTO> Logout(string operatorToken);
        Task<AuthResponseDTO> Authenticate(AuthRequestDTO request);
        Task<BetPlacementResponseDTO> BetPlacement(OperatorBetRequestDTO request, string clientId, string operatorToken, string isAgent);
        Task<BetSettlementResponseDTO> BetSettlement(OperatorBetSettlementRequestDTO request, string clientId);
        Task<BetSettlementResponseDTO> CashOutBet(OperatorBetSettlementRequestDTO request, string clientId, string operatorToken, string isAgent);
        Task<BetSettlementResponseDTO> BetCancellation(OperatorBetCancellationRequestDTO request, string clientId);
    }
}
