using Platform.Shared.Enums;

namespace Platform.Shared.MessageModels
{
    public class OnUOFBetResettlement
    {
        public long SelectionId { get; set; }
        public BetStatus SelectionStatus { get; set; }
        public string SettlementHomeScore { get; set; }
        public string SettlementAwayScore { get; set; }
    }
}
