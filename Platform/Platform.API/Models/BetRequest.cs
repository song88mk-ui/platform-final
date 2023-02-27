using Platform.Shared.Enums;
using System.Collections.Generic;

namespace Platform.API.Models
{
    public class BetRequest
    {
        public BetRequest()
        {
            Selections = new List<BetSelectionRequest>();
        }
        public decimal Stake { get; set; }
        public OddsType OddsType { get; set; }
        public OddsChangeType OddsChangeType { get; set; }
        public Channel Channel { get; set; }
        public string IPAddress { get; set; }
        public bool IsBoosted { get; set; } = false;
        public bool IsBetBuilder { get; set; } = false;
        public string BonusId { get; set; } = string.Empty;
        public decimal TotalOdds { get; set; }
        public List<BetSelectionRequest> Selections { get; set; }
    }

    public class BetSelectionRequest
    {
        public int SportId { get; set; }
        public string FixtureId { get; set; }
        public string MarketId { get; set; }
        public string SelectionId { get; set; }
        public decimal Odds { get; set; }
        public string CorrelationId { get; set; }
    }
}
