using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class ClientSegmentLeagueOrder
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string SportName { get; set; }
        public string SegmentName { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
    }
}
