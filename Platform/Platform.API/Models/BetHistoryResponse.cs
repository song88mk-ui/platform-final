using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.API.Models
{
    public class BetHistoryResponse
    {
        public BetHistoryResponse()
        {
            Bets = new List<BetHistory>();
        }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public List<BetHistory> Bets { get; set; }
    }

    public class BetHistory
    {
        public BetHistory()
        {
            Selections = new List<BetSelectionHistory>();
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
        public List<BetSelectionHistory> Selections { get; set; }
    }

    public class BetSelectionHistory
    {
        public string BetSelectionId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public int MarketTemplateId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public decimal Line { get; set; }
        public int SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
        public string SelectionName { get; set; }
        public decimal Odds { get; set; }
        public FixtureStatus FixtureStatus { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string BetHomeScore { get; set; }
        public string BetAwayScore { get; set; }
        public string SettlementHomeScore { get; set; }
        public string SettlementAwayScore { get; set; }
    }
}
