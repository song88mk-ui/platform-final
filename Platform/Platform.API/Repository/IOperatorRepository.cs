using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<LoginResponseDTO>> Login(string userName, string password, string endpoint);
        Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent);
        Task<BaseResponseDTO<OperatorBetResponseDTO>> ResendCancellation(string endpoint, string info);
    }
}
