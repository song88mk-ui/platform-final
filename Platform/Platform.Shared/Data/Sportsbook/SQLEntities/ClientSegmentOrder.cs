using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class ClientSegmentOrder
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int SortOrder { get; set; }
    }
}
