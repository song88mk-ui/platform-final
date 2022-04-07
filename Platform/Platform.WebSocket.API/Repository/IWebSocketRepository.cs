using Microsoft.AspNetCore.SignalR;
using Platform.WebSocket.API.Messages;
using System.Threading.Tasks;

namespace Platform.WebSocket.API.Repository
{
    public interface IWebSocketRepository
    {
        Task PublishOddsChange(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, IncomingOddsChange message);
        Task PublishBetStopFixture(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, IncomingBetStopFixture onBetStopFixture);
        Task PublishBetStopGlobal(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, IncomingBetStopGlobal onBetStopGlobal);
    }
}
