using Platform.Operator.API.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IAuthRepository
    {
        Task<BaseResponse<AuthResponse>> Authenticate(string operatorToken);
    }
}
