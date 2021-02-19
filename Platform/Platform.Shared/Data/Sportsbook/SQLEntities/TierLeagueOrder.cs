namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class TierLeagueOrder
    {
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string SegmentName { get; set; }
        public string LeagueName { get; set; }
        public int Tier { get; set; }
        public int SortOrder { get; set; }
    }
}
