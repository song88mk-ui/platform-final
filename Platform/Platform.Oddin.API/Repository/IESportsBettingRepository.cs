using Platform.Oddin.API.Models;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Repository
{
    public interface IESportsBettingRepository
    {
        Task<UserDetailsResponse> GetUserDetails(UserDetailsRequest request);
        Task<CreditUserResponse> CreditUser(CreditUserRequest request);
        Task<DebitUserResponse> DebitUser(DebitUserRequest request);
    }
}
