using System;

namespace Platform.Shared.Data.Betting.Entities
{
    public class BetPriceBoost
    {
        public Guid BetId { get; set; }
        public string UserId { get; set; }
        public long FixtureId { get; set; }
        public long MarketId { get; set; }
    }
}
