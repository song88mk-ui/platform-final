using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Repository
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent);
    }
}
