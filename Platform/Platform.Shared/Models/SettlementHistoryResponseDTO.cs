using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class SettlementHistoryResponseDTO
    {
        public SettlementHistoryResponseDTO()
        {
            Bets = new List<SettlementHistoryDTO>();
        }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public List<SettlementHistoryDTO> Bets { get; set; }
    }

    public class SettlementHistoryDTO
    {
        public SettlementHistoryDTO()
        {
            Selections = new List<BetSelectionHistoryDTO>();
        }
        public string BetId { get; set; }
        public decimal Stake { get; set; }
        public decimal Odds { get; set; }
        public decimal Payout { get; set; }
        public decimal WinLoss { get; set; }
        public decimal Commission { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string OddsType { get; set; }
        public string CurrencyCode { get; set; }
        public int CashOutStatusId { get; set; }
        public string CashOutStatusName { get; set; }
        public decimal CashOutStake { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public decimal StakeTax { get; set; }
        public decimal WinningsTax { get; set; }
        public decimal TaxedStake { get; set; }
        public decimal TaxedWinnings { get; set; }
        public decimal ParlayBonus { get; set; }
        public decimal ParlayBonusWinnings { get; set; }
        public decimal StakeReal { get; set; }
        public decimal StakeBonus { get; set; }
        public decimal StakeRealPayout { get; set; }
        public decimal StakeBonusPayout { get; set; }
        public bool IsFreeBet { get; set; }
        public bool IsBoosted { get; set; }
        public List<BetSelectionHistoryDTO> Selections { get; set; }
    }

}
