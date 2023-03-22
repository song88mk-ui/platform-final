using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Repository
{
    public interface IWalletRepository
    {
        Task<WalletResponse> Credit(CreditOrDebitRequestDTO request);
        Task<WalletResponse> Debit(CreditOrDebitRequestDTO request);
    }
}
