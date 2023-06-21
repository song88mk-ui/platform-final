using Platform.Oddin.API.Enums;
using Platform.Oddin.API.Models;
using Platform.Shared.Models;
using System;
using System.Threading.Tasks;

using Platform.Oddin.API.Models;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Repository
{
    public interface IESportsBettingRepository
    {
        Task<Tuple<SessionCode, UserDetailsResponse>> GetUserDetails(UserDetailsRequest request);
        Task<CreditUserResponse> Deposit(CreditUserRequest request);
        Task<DebitUserResponse> Withdraw(DebitUserRequest request);
    }
}
