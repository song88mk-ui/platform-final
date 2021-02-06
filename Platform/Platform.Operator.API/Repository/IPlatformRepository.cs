using Platform.Operator.API.Models;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IPlatformRepository
    {
        Task<AuthenticationResponseDTO> Authenticate(string operatorToken);
    }
}
