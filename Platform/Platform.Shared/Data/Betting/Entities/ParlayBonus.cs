using System;

namespace Platform.Shared.Data.Betting.Entities
{
    public class ParlayBonus
    {
        public Guid ClientId { get; set; }
        public long ClientIdRef { get; set; } 
        public int NoOfSelection { get; set; }
        public decimal Bonus { get; set; }
        public decimal MinOdds { get; set; }

    }

    public class ParlayBonusAudit
    {
        public long Id { get; set; }
        public Guid ClientId { get; set; }
        public long ClientIdRef { get; set; }
        public int NoOfSelection { get; set; }
        public decimal Bonus { get; set; }
        public decimal MinOdds { get; set; }
        public DateTime TimeStamp { get; set; }

    }
}
