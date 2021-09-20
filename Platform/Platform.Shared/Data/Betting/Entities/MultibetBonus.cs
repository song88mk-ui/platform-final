using System;

namespace Platform.Shared.Data.Betting.Entities
{
    public class MultibetBonus
    {
        public Guid ClientId { get; set; }
        public long ClientIdRef { get; set; } 
        public int NoOfGames { get; set; }
        public decimal Bonus { get; set; }
    }

    public class MultibetBonusAudit
    {
        public long Id { get; set; }
        public Guid ClientId { get; set; }
        public long ClientIdRef { get; set; }
        public int NoOfGames { get; set; }
        public decimal Bonus { get; set; }
        public DateTime TimeStamp { get; set; }

    }
}
