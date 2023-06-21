using Platform.Oddin.API.Models;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Repository
{
    public interface IWalletRepository
    {
        Task<WalletResponse> Credit(CreditOrDebitRequestDTO request);
        Task<WalletResponse> Debit(CreditOrDebitRequestDTO request);
    }
}
