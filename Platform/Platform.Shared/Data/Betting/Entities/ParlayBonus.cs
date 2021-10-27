using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Betting.Entities
{

    public class ParlayBonusAudit
    {
        public long Id { get; set; }
        public Guid ClientId { get; set; }
        public long ClientIdRef { get; set; }
        public int SelectionCount { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal ParlayBonus { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal ParlayBonusMinOdds { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal ParlayBonusMaxWinnings { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
