using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class BetRecommendationRequestDTO
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public string CurrencyCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
}
