using Platform.Shared.Enums;
using System.Collections.Generic;

namespace Platform.API.Models
{
    public class BetRequest2
    {
        public BetRequest2()
        {
            Selections = new List<BetSelectionRequest2>();
        }
        public decimal Stake { get; set; }
        public OddsType OddsType { get; set; }
        public OddsChangeType OddsChangeType { get; set; }
        public Channel Channel { get; set; }
        public string IPAddress { get; set; }
        public List<BetSelectionRequest2> Selections { get; set; }
    }

    public class BetSelectionRequest2
    {
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public long MarketId { get; set; }
        public long SelectionId { get; set; }
        public decimal Odds { get; set; }
        public string CorrelationId { get; set; }
    }
}
