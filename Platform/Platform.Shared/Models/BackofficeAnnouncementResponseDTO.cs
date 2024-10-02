using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{

    public class BackofficeAnnouncementResponseDTO
    {
        public BackofficeAnnouncementResponseDTO()
        {
            Announcements = new List<BackofficeAnnouncementDTO>();
        }
        public List<BackofficeAnnouncementDTO> Announcements { get; set; }
    }

    public class BackofficeAnnouncementDTO : AnnouncementDTO
    {
        public List<string> ClientIds { get; set; }
        public bool IsActive { get; set; }
    }

}
