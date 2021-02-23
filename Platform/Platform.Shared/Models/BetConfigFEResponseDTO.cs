using System.Collections.Generic;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Platform.Shared.Models
{
    public class BetConfigFEResponseDTO
    {
        public BetConfigFEResponseDTO()
        {
            OverrideSportConfig = new List<OverrideSportConfigFE>();
        }
        public DefaultSportConfigFE DefaultSportConfig { get; set; }
        public List<OverrideSportConfigFE> OverrideSportConfig { get; set; }
        public ParlayConfigFE Parlayconfig { get; set; }
    }

    public class OverrideSportConfigFE
    {
        [JsonProperty("sportId")] 
        public int SportId { get; set; }
        [JsonProperty("sportName")]
        public string SportName { get; set; }
        [JsonProperty("minStake")]
        public decimal MinStake { get; set; }
        [JsonProperty("maxStake")]
        public decimal MaxStake { get; set; }

    }

    public class DefaultSportConfigFE
    {
        [JsonProperty("minStake")] 
        public decimal MinStake { get; set; }
        [JsonProperty("maxStake")]
        public decimal MaxStake { get; set; }
    }

    public class ParlayConfigFE
    {
        [JsonProperty("minStake")] 
        public decimal MinStake { get; set; }
        [JsonProperty("maxStake")]
        public decimal MaxStake { get; set; }
        [JsonProperty("maxPayout")]
        public decimal MaxPayout { get; set; }
    }
}
