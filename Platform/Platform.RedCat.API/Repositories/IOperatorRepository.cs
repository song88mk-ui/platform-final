using Platform.RedCat.API.Models;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.RedCat.API.Repositories
{
    public interface IOperatorRepository
    {
        Task<RedCatBaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent);
    }
}
