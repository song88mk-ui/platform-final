using Platform.WebSocket.API.Messages;
using System.Threading.Tasks;

namespace Platform.WebSocket.API
{
    public interface IPlatformEvent
    {
        Task OnOddsChange(OddsChangeFixture message);
        Task OnBetStopFixture(BetStopFixture message);
        Task OnBetStopGlobal(OutgoingBetStopGlobal message);
        Task OnBetslipOddsChange(OddsChangeFixture message);
        Task OnBetslipBetStopFixture(BetStopFixture message);
        Task OnBetStatus(OutgoingBetStatus message);
        Task OnCashOutChange(OutgoingCashOutChange message);
        Task OnWalletChange(OutgoingWalletChange message);
        Task OnPayableChange(OutgoingPayableChange message);
    }
}
