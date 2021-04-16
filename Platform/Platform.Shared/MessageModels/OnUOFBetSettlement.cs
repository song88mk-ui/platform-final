using Platform.Shared.Enums;

namespace Platform.Shared.MessageModels
{
    public class OnUOFBetSettlement
    {
        public long SelectionId { get; set; }
        public BetStatus SelectionStatus { get; set; }
        public string SettlementScore { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
    }
}
