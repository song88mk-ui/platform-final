namespace Platform.Shared.Models
{
    public class LeagueHighlightsResponseDTO
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int FixtureCount { get; set; }
    }
}
