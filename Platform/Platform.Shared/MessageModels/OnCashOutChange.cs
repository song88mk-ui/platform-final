using Platform.Shared.Enums;

namespace Platform.Shared.MessageModels
{
    public class OnCashOutChange
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public string BetId { get; set; }
        public CashOutStatus CashOutStatus { get; set; }
        public decimal CashOutStake { get; set; }
    }
}
