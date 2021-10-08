using System;

namespace Platform.Shared.Data.Betting.Entities
{

    public class ParlayBonusAudit
    {
        public long Id { get; set; }
        public Guid ClientId { get; set; }
        public long ClientIdRef { get; set; }
        public int SelectionCount { get; set; }
        public decimal ParlayBonus { get; set; }
        public decimal MinOdds { get; set; }
        public DateTime TimeStamp { get; set; }

    }
}
