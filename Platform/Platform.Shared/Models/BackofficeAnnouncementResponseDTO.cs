using Platform.Shared.Enums;
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
        public BackofficeAnnouncementDTO()
        {
            Languages = new List<LanguageCode>();
        }

        public int Id { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public List<ClientInfo> Clients { get; set; }
        public List<LanguageCode> Languages { get; set; }
        public bool IsActive { get; set; }
    }

}
