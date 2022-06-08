using Microsoft.AspNetCore.SignalR;
using Platform.Shared.Utility;
using Platform.WebSocket.API.Messages;
using System.Threading.Tasks;

namespace Platform.WebSocket.API.Repository
{
    public interface IWebSocketRepository
    {
        Task PublishOddsChangeForMainMarkets(string clientCode, OutgoingOddsChange message);
        Task PublishOddsChangeForAllMarkets(string clientCode, OutgoingOddsChange message);
        Task PublishBetStopFixture(string clientCode, OutgoingBetStopFixture onBetStopFixture);
        Task PublishBetStopGlobal(string clientCode, OutgoingBetStopGlobal onBetStopGlobal);
    }
}
