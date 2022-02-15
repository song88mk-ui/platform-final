using Platform.WebSocket.API.Messages;
using System.Threading.Tasks;

namespace Platform.WebSocket.API
{
    public interface IPlatformEvent
    {
        Task OnOddsChange(OutgoingOddsChange message);
        Task OnBetStopFixture(OutgoingBetStopFixture message);
        Task OnBetStopGlobal(OutgoingBetStopGlobal message);
    }
}
