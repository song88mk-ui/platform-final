using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class BackofficeAnnouncementRequestDTO
    {
        public LanguageCode LanguageCode { get; set; }
        public string ClientIds { get; set; }
        public string SearchText { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; } = 20;
    }
}
