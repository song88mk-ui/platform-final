using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Betting.Entities
{
    public class BetConfig
    {
        [Key]
        public Guid ClientId { get; set; }

        public int SportsId { get; set; }

        public int CategoryId { get; set; }

        public decimal ReferenceBet { get; set; }

        public decimal MinBet { get; set; }
        public decimal MaxBet { get; set; }
        public decimal MaxPerGame { get; set; }

        [Column(TypeName = "decimal(30,18)")]
        public decimal CompanyShare { get; set; }

        public DateTime TransactionDate { get; set; }
        public DateTime EffectivityDate { get; set; }

        public string CurrencyCode { get; set; }
    }
}
