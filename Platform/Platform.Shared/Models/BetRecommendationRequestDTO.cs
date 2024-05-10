using System;

namespace Platform.Shared.Models
{
    public class BetRecommendationRequestDTO
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
    }
}
