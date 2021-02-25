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
        public BetStatus BetStatus { get; set; }
        public OddsType OddsType { get; set; }
        public CurrencyCode CurrencyCode { get; set; }
        public DateTime TransactionDate { get; set; }
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
        public FixtureStatus FixtureStatus { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string BetScore { get; set; }
        public string SettlementScore { get; set; }
        public string BetHomeScore { get; set; }
        public string BetAwayScore { get; set; }
        public string SettlementHomeScore { get; set; }
        public string SettlementAwayScore { get; set; }
    }
}
