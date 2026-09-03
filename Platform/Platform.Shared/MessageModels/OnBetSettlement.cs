using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
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
        public int ChannelTypeId { get; set; } = (int)ChannelType.BG;
        public string Message { get; set; }
        public bool IsRetry { get; set; }
        public decimal NonTaxedPayout { get; set; }
        public bool IsAudit { get; set; } = false;
        public bool IsResettle { get; set; } = false;

        // Cumulative cashout figures across every cashout of the bet, set by
        // Platform.CashOut.Service on OnBetFullCashOut/OnBetPartialCashOut publishes.
        // Bet.TrueStake/Payout only carry post-cashout per-event values, while the GTS
        // bet update needs totals (totalStake, cashedOutTotalStake, cashedOutAmount).
        // Null on messages from other publishers and on pre-enrichment messages still
        // in flight; consumers must skip cashout reporting when these are null.
        public decimal? OriginalTrueStake { get; set; }
        public decimal? CashedOutTotalTrueStake { get; set; }
        public decimal? CashedOutTotalAmount { get; set; }
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
