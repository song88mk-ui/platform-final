using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Data.Betting.Entities
{
    public class BetConfig
    {
        public long Id { get; set; }
        public Guid ClientId { get; set; }
        public int SportsId { get; set; } // 999 default config
        public int CategoryId { get; set; } // Soccer, Parlay etc
        public decimal ReferenceBet { get; set; } // 0.0001 default for EUR currency
        public decimal MinBet { get; set; }
        public decimal MaxBet { get; set; }
        public decimal MaxPerGame { get; set; }
        public decimal CompanyShare { get; set; } // Combine with EffectivityDate to get the correct Companyshare
        public DateTime TransactionDate { get; set; }
        public DateTime EffectivityDate { get; set; } //Take into account the effectivity date of company share change from BO 
        public string CurrencyCode { get; set; }
    }
}
