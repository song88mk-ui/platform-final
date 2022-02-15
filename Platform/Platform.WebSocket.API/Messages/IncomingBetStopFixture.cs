using System.Collections.Generic;

namespace Platform.WebSocket.API.Messages
{
    public class IncomingBetStopFixture
    {
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public bool IsBetStop { get; set; }
        public List<int> MarketTemplateGroups { get; set; }
        public string CorrelationId { get; set; }
    }
}
