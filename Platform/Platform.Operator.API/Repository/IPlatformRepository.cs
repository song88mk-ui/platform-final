using Platform.Operator.API.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IPlatformRepository
    {
        Task<AuthenticationResponse> Authenticate(string operatorToken);
    }
}
