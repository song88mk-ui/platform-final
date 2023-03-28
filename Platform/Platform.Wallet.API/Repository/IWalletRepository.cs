using Platform.Shared.Models;
using Platform.Wallet.API.Models;
using System;
using System.Threading.Tasks;

namespace Platform.Wallet.API.Repository
{
    public interface IWalletRepository
    {
        Task<Tuple<int, string, decimal>> CreditUserWallet(CreditOrDebitRequestDTO request);
        Task<Tuple<int, string, decimal>> DebitUserWallet(CreditOrDebitRequestDTO request);
        Task<Tuple<int, string, decimal, decimal>> TransferUserWallet(TransferUserWalletRequest request);
        Task<Tuple<int, string, decimal, decimal, string>> GetUserWallet(UserWalletRequest request);
    }
}
