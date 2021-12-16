using System;

namespace Platform.Shared.Data.Betting.Entities
{
    public class BetConfig
    {
        public Guid ClientId { get; set; }
        public string CurrencyCode { get; set; } 
        public string DefaultConfig { get; set; }
        public string OverrideConfig { get; set; }
        public string ParlayConfig { get; set; }
        public string TaxConfig { get; set; }
        public string PriceboostConfig { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
