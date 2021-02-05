using Platform.API.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IPlatformRepository
    {
        Task<BaseResponse<WalletBalanceResponse>> GetWalletBalance(string clientId, string operatorToken);
    }
}
