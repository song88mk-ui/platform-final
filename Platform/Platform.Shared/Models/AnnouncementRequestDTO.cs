using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class AnnouncementRequestDTO
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
}
