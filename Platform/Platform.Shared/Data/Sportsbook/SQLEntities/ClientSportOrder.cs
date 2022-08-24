using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class ClientSportOrder
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
