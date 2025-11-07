using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnBetSettlement
    {
        public OnBetSettlement()
        {
            BetSelections = new List<BetSelection>();
            IsRetry = false;
        }

        public Bet Bet { get; set; }
        public List<BetSelection> BetSelections { get; set; }
        public string Message { get; set; }
        public bool IsRetry { get; set; }
        public decimal NonTaxedPayout { get; set; }
        public bool IsAudit { get; set; } = false;
        public bool IsResettle { get; set; } = false;
    }

    public class OnBetSettlementPartial : OnBetSettlement
    {
        public decimal CashoutStake {  get; set; }
        public decimal CashoutStakePercent { get; set; }
        public decimal Stake { get; set; }
        public decimal TrueStake { get; set; }
        public decimal TaxedStake { get; set; }
    }

    public class OnBetSettlementRetry
    {
        public int RemainingRetries { get; set; }

        public OnBetSettlement OnBetSettlement { get; set; }
    }
}
