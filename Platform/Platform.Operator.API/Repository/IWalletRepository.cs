using Platform.Operator.API.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IWalletRepository
    {
        Task<WalletResponse> GetUserWallet(UserWalletRequest request);
        Task<BetResponse> PlaceBet(CreditOrDebitRequest request);
        Task<BetResponse> SettleBet(CreditOrDebitRequest request);
        Task<BetResponse> CashOutBet(CreditOrDebitRequest request);
        Task<BetResponse> CancelBet(CreditOrDebitRequest request);
    }
}
