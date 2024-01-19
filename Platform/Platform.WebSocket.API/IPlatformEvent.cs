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

        //// using MessagePack C# see: https://github.com/MessagePack-CSharp/MessagePack-CSharp
        //Task OnOddsChange2(byte[] message);
        //Task OnBetStopFixture2(byte[] message);
        //Task OnBetStopGlobal2(byte[] message);
        //Task OnBetslipOddsChange2(byte[] message);
        //Task OnBetslipBetStopFixture2(byte[] message);
        //Task OnBetStatus2(byte[] message);
        //Task OnCashOutChange2(byte[] message);
        //Task OnWalletChange2(byte[] message);
        //Task OnPayableChange2(byte[] message);
    }
}
