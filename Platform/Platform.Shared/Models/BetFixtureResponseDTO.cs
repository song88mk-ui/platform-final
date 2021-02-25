using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetFixtureResponseDTO
    {
        public BetFixtureResponseDTO()
        {
            Selections = new List<BetFixtureSelectionResponseDTO>();
        }
        public List<BetFixtureSelectionResponseDTO> Selections { get; set; }
    }

    public class BetFixtureSelectionResponseDTO
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public FixtureType FixtureType { get; set; }
        public string MarketTemplateId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public string SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
        public string SelectionName { get; set; }
        public decimal Odds { get; set; }
        public FixtureStatus FixtureStatus { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string Score { get; set; }
        public string FixtureSpecifier { get; set; }
        public string SelectionSpecifier { get; set; }
        public string CorrelationId { get; set; }
        public BetSelectionState BetSelectionState { get; set; }
    }
}
