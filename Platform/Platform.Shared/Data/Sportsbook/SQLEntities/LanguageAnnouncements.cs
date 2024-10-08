using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class LanguageAnnouncements
    {
        public int AnnouncementId { get; set; }
        public string LangCode { get; set; }
        public string Description { get; set; }

        public Announcement Announcements { get; set; }
    }
}
