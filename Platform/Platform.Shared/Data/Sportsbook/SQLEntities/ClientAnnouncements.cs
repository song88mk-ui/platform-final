using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class ClientAnnouncements
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public int AnnouncementId { get; set; }

        public Announcement Announcements { get; set; }
    }
}
