using Platform.Shared.Enums;

namespace Platform.Shared.MessageModels
{
    public class OnUOFBetCancellation
    {
        public long MarketId { get; set; }
        public string Reason { get; set; }
        public string SettlementHomeScore { get; set; }
        public string SettlementAwayScore { get; set; }
        public BetStatus MarketStatus { get; set; }
    }
}
