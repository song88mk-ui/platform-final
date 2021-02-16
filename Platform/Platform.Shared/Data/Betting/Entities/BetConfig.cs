using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Data.Betting.Entities
{
    public class BetConfig
    {
        public Guid ClientId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal StakeLimitFixture { get; set; }
        public decimal MaxParlayPayout { get; set; }
        public string DefaultConfig { get; set; }
        public string OverrideConfig { get; set; }
        public string ParlayConfig { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
