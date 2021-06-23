using Platform.Shared.Enums;
using System;

namespace Platform.Shared.MessageModels
{
    public class OnCashoutBetSettlement
    {
        public Guid BetId { get; set; }
        public CashoutType CashoutType { get; set; }
        public decimal CashOutStake { get; set; }
        public decimal TaxedStake { get; set; }
        public decimal TaxedWinnings { get; set; }
    }
}
