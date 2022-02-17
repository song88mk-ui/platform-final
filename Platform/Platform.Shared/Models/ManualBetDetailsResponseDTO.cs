using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ManualBetDetailsResponseDTO
    {
        public ManualBetDetailsResponseDTO()
        {
            Bets = new List<BetDetailsDTO>();
        }
        public List<BetDetailsDTO> Bets { get; set; }
    }

    public class BetDetailsDTO
    {
        public BetDetailsDTO()
        {
            Selections = new List<BetSelectionDetailsDTO>();
        }
        public string BetId { get; set; }
        public int BetStatusId { get; set; }
        public int ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string BetStatusName { get; set; }
        public string CurrencyCode { get; set; }
        public string Reason { get; set; }
        public DateTime BetDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public string UserId { get; set; }
        public bool IsFreeBet { get; set; }
        public bool IsBoosted { get; set; }
        public List<BetSelectionDetailsDTO> Selections { get; set; }
    }

    public class BetSelectionDetailsDTO
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
    }
}
