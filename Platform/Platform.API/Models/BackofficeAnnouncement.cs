using Platform.Shared.Enums;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BackofficeAnnouncement : AnnouncementDTO
    {
        public int Id { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public List<string> ClientCodes { get; set; }
        public bool IsActive { get; set; }
    }
}
