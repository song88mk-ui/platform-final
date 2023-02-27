using Platform.Wallet.API.Models;
using System;
using System.Threading.Tasks;

namespace Platform.Wallet.API.Repository
{
    public interface IWalletRepository
    {
        Task<Tuple<int, string, decimal>> CreditUserWallet(CreditOrDebitRequest request);
        Task<Tuple<int, string, decimal>> DebitUserWallet(CreditOrDebitRequest request);
        Task<Tuple<int, string, decimal, decimal>> TransferUserWallet(TransferUserWalletRequest request);
        Task<Tuple<int, string, decimal, string>> GetUserWallet(UserWalletRequest request);
    }
}
