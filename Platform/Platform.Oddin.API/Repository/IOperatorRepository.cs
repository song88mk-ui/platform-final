using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Repository
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent);
        Task<BaseResponseDTO<OperatorBetResponseDTO>> Debit(string clientId, string operatorToken, string endpoint, bool isAgent, OperatorESportsDebitRequestDTO request);
        Task<BaseResponseDTO<OperatorBetResponseDTO>> Credit(string clientId, string endpoint, OperatorESportsCreditRequestDTO request);
    }
}
