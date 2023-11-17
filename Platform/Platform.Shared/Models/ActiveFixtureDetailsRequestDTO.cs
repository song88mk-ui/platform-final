using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ActiveFixtureDetailsRequestDTO
    {
        public string FixtureId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public string ClientCode { get; set; }
        public string IpCountryCode { get; set; }
    }

}
