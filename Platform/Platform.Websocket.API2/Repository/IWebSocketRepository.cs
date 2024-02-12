using Platform.WebSocket.API2.Messages;

namespace Platform.WebSocket.API2.Repository
{
    public interface IWebSocketRepository
    {
        Task PublishOddsChangeForMainMarkets(string clientCode, OutgoingOddsChange message);
        Task PublishOddsChangeForAllMarkets(string clientCode, OutgoingOddsChange message);
        Task PublishBetStopFixture(string clientCode, OutgoingBetStopFixture onBetStopFixture);
        Task PublishBetStopGlobal(string clientCode, OutgoingBetStopGlobal onBetStopGlobal);
    }
}
