using System;

namespace Platform.Shared.Data.Platform.Entities
{
    public class TestConfig
    {
        public Guid ClientId { get; set; }
        public bool IsFreeBet { get; set; }
        public decimal StakeRealPercent { get; set; }
        public decimal StakeBonusPercent { get; set; }
    }
}
