using Platform.Operator.API.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IAuthRepository
    {
        Task<BaseResponse<LoginResponse>> Login(LoginRequest request, string userAgent = "", string ipAddress = "", string country = "");
        Task<BaseResponse<bool>> Logout(string operatorToken);
        Task<BaseResponse<AuthResponse>> Authenticate(string operatorToken, string isAgent);
    }
}
