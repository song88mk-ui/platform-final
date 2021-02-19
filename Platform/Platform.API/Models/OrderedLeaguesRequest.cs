using Platform.Shared.Enums;

namespace Platform.API.Models
{
    public class OrderedLeaguesRequest
    {
        public int SportId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }

    public class TempOrderedLeaguesRequest
    {
        public int SportId { get; set; }
    }
}
