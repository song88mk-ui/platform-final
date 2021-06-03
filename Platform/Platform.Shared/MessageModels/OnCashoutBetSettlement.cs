using Platform.Shared.Enums;
using System;

namespace Platform.Shared.MessageModels
{
    public class OnCashoutBetSettlement
    {
        public Guid BetId { get; set; }
        public CashoutType CashoutType { get; set; }
        public decimal Cashout { get; set; }
        public decimal StakeUsed { get; set; }
    }
}
