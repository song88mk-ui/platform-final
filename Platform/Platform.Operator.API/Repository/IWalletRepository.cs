using Platform.Operator.API.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IWalletRepository
    {
        Task<WalletResponse> GetUserWallet(UserWalletRequest request);
    }
}
