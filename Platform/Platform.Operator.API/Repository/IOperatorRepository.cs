using Platform.Operator.API.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IOperatorRepository
    {
        Task<AuthResponseDTO> Authenticate(string clientId, string operatorToken);
    }
}
