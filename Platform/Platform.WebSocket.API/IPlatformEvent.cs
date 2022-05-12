using Platform.WebSocket.API.Messages;
using System.Threading.Tasks;

namespace Platform.WebSocket.API
{
    public interface IPlatformEvent
    {
        Task OnOddsChange(OutgoingOddsChange message);
        Task OnOddsChange(OutgoingOddsChangeDelta message);
        Task OnBetStopFixture(OutgoingBetStopFixture message);
        Task OnBetStopGlobal(OutgoingBetStopGlobal message);
        Task OnBetslipOddsChange(OutgoingOddsChange message);
        Task OnBetslipOddsChange(OutgoingOddsChangeDelta message);
        Task OnBetslipBetStopFixture(OutgoingBetStopFixture message);
        Task OnBetStatus(OutgoingBetStatus message);
        Task OnCashOutChange(OutgoingCashOutChange message);
        Task OnWalletChange(OutgoingWalletChange message);
    }
}
