using Platform.Shared.Enums;

namespace Platform.Placement.Service.Entities
{
    public class OnSettlementMessage
    {
        public int MarketTemplateId { get; set; }
        public long SelectionId { get; set; }
        public BetStatus SettlementStatus { get; set; }
    }
}
