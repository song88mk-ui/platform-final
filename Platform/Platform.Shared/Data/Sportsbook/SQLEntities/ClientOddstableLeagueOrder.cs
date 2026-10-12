using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    // Per-client league order for the Oddstable (the Early, Today and Live fixture lists), written by
    // backoffice-final (BO-107). It is separate from ClientLeagueOrder, which orders the Sport Tree, so
    // changing one never moves the other. One full replace per client and sport.
    public class ClientOddstableLeagueOrder
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
    }
}
