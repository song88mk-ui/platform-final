using Microsoft.AspNetCore.SignalR;
using Platform.Shared.Utility;
using Platform.WebSocket.API.Messages;
using System.Threading.Tasks;

namespace Platform.WebSocket.API.Repository
{
    public interface IWebSocketRepository
    {
        Task PublishOddsChange(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, string clientCode, OutgoingOddsChange message);
        Task PublishBetStopFixture(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, string clientCode, OutgoingBetStopFixture onBetStopFixture);
        Task PublishBetStopGlobal(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, string clientCode, OutgoingBetStopGlobal onBetStopGlobal);
    }
}
