using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{

    public class AnnouncementResponseDTO
    {
        public AnnouncementResponseDTO()
        {
            Announcements = new List<AnnouncementDTO>();
        }
        public List<AnnouncementDTO> Announcements { get; set; }
    }

    public class AnnouncementDTO
    {
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Message { get; set; }
    }

    public class DescriptionTranslation
    {
        public string LangCode { get; set; }
        public string Message { get; set; }
    }
}
