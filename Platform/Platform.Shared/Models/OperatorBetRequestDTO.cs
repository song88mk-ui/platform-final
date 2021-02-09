using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class OperatorBetRequestDTO
    {
        public OperatorBetRequestDTO()
        {
            BetSelections = new List<OperatorBetSelectionRequestDTO>();
        }
        public string TransactionId { get; set; }
        public string BetId { get; set; }
        public string UserId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Stake { get; set; }
        public decimal Odds { get; set; }
        public string BetStatus { get; set; }
        public DateTime TransactionDate { get; set; }
        public List<OperatorBetSelectionRequestDTO> BetSelections { get; set; }
    }

    public class OperatorBetSelectionRequestDTO
    {
        public string BetSelectionId { get; set; }
        public string BetSelectionStatus { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureId { get; set; }
        public string FixtureName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string FixtureStatus { get; set; }
        public string Score { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public string SelectionId { get; set; }
        public string SelectionName { get; set; }
        public decimal Odds { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
    }
}
