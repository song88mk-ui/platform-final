using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class BetRecommendationDetailRequestDTO
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public string IpCountryCode { get; set; }
        public List<long> FixtureIds { get; set; }
    }
}
