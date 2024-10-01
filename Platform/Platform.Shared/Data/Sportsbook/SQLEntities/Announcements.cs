using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class Announcements
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ClientCodes { get; set; }
        public bool IsActive { get; set; }
    }
}
