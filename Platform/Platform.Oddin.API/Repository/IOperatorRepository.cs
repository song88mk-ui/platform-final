using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Repository
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent);
        Task<BaseResponseDTO<OperatorBetResponseDTO>> PlaceBet(string clientId, string operatorToken, string endpoint, bool isAgent, OperatorESportsBetPlacementRequestDTO request);
        Task<BaseResponseDTO<OperatorBetResponseDTO>> SettleBet(string clientId, string endpoint, OperatorESportsBetSettlementRequestDTO request);
    }
}
