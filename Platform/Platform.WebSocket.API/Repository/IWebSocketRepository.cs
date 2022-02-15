using Microsoft.AspNetCore.SignalR;
using Platform.WebSocket.API.Messages;
using System.Threading.Tasks;

namespace Platform.WebSocket.API.Repository
{
    public interface IWebSocketRepository
    {
        Task PublishOddsChange(IHubContext<PlatformHub, IPlatformEvent> hubContext, IncomingOddsChange message);
        Task PublishBetStopFixture(IHubContext<PlatformHub, IPlatformEvent> hubContext, IncomingBetStopFixture onBetStopFixture);
        Task PublishBetStopGlobal(IHubContext<PlatformHub, IPlatformEvent> hubContext, IncomingBetStopGlobal onBetStopGlobal);
    }
}
