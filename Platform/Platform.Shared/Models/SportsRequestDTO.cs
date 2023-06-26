using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class SportsRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public string ClientCode { get; set; }
        public SportGroup SportGroup { get; set; }
        public FixtureScope? FixtureScope { get; set; }
    }
}
