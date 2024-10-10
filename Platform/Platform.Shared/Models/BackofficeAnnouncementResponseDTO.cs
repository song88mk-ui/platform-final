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
        public int TotalCount { get; set; }
        public int TotalPage { get; set; }
    }

    public class BackofficeAnnouncementDTO : AnnouncementDTO
    {
        public BackofficeAnnouncementDTO()
        {
            Languages = new List<AnnouncementMessage>();
        }

        public int Id { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public List<ClientInfo> Clients { get; set; }
        public List<AnnouncementMessage> Languages { get; set; }
        public bool IsActive { get; set; }
        public string UserId { get; set; }
        public string UserRoleId { get; set; }
    }

}
