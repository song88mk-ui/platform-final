using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class AddOrUpdateBackofficeAnnouncementRequest
    {
        public int Id { get; set; }
        public List<string> ClientCodes { get; set; }
        public List<AnnouncementMessage> AnnouncementMessages { get; set; }
        public bool IsActive { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
