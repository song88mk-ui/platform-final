using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Betting.Entities
{
    public class BetSelectionAudit
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public Guid BetSelectionId { get; set; }
        public string UserId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public int FixtureTypeId { get; set; }
        public string FixtureTypeName { get; set; }
        public string MarketTemplateId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public string SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
        public string SelectionName { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public string Line { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Odds { get; set; }
        public int BetSelectionStatusId { get; set; }
        public string BetSelectionStatusName { get; set; }
        public bool IsSettled { get; set; }
        public bool IsResettled { get; set; }
        public string FixtureSpecifier { get; set; }
        public string SelectionSpecifier { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string BetScore { get; set; }
        public string SettlementScore { get; set; }
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public string CorrelationId { get; set; }
    }
}
