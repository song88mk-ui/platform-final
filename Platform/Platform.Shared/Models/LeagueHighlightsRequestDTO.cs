using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class LeagueHighlightsRequestDTO
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class LeagueHighlightsFixturesRequestDTO : LeagueHighlightsRequestDTO
    {
        public string IpCountryCode { get; set; }
        public string Device { get; set; }
    }
}
