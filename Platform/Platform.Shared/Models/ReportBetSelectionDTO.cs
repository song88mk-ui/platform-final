using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class ReportBetSelectionDTO
    {
        public Guid BetId { get; set; }
        public Guid BetSelectionId { get; set; }
        public int ClientId { get; set; }
        public string UserId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public long SelectionId { get; set; }
        public string SelectionName { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public decimal Line { get; set; }
        public decimal Odds { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public bool IsSettled { get; set; }
        public bool IsResettled { get; set; }
        public string FixtureSpecifier { get; set; }
        public string SelectionSpecifier { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public int MarketTemplateGroupId { get; set; }
        public int MarketTemplateId { get; set; }
        public int SelectionTemplateId { get; set; }
        public string Score { get; set; }
        public string BetScore { get; set; }
        public string FinalScore { get; set; }
        public string HalftimeScore { get; set; }
    }
}
