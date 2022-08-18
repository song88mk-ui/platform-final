using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetHistoryResponseDTO
    {
        public BetHistoryResponseDTO()
        {
            Bets = new List<BetHistoryDTO>();
        }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal TotalWinLoss { get; set; }
        public decimal TotalWLC { get; set; }
        public List<BetHistoryDTO> Bets { get; set; }
    }

    public class BetHistoryDTO
    {
        public BetHistoryDTO()
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

    public class BetSelectionHistoryDTO
    {
        public string BetSelectionId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureId { get; set; }
        public string FixtureName { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionId { get; set; }
        public string SelectionName { get; set; }
        public decimal Odds { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string BetScore { get; set; }
        public string SettlementScore { get; set; }
        public int BetSelectionStatusId { get; set; }
        public string BetSelectionStatusName { get; set; }
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public decimal DeadHeatFactor { get; set; }
    }
}
