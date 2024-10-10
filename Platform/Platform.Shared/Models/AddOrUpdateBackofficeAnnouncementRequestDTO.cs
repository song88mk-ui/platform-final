using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class AddOrUpdateBackofficeAnnouncementRequestDTO
    {
        public int Id { get; set; }
        public List<ClientInfo> Clients { get; set; }
        public List<AnnouncementMessage> AnnouncementMessages { get; set; }
        public bool IsActive { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string UserId { get; set; }
        public string UserRoleId { get; set; }
    }
    public class AnnouncementMessage
    {
        public LanguageCode LanguageCode { get; set; }
        public string Message { get; set; }
    }
}
