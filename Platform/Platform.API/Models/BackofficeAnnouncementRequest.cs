using Platform.Shared.Enums;

namespace Platform.Shared.Models
{
    public class BackofficeAnnouncementRequest
    {
        public LanguageCode LanguageCode { get; set; }
        public string ClientCodes { get; set; }
        public string SearchText { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; } = 20;
    }
}
