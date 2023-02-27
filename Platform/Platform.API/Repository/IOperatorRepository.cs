using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint);
        Task<BaseResponseDTO<OperatorBetResponseDTO>> ResendCancellation(string endpoint, string info);
    }
}
