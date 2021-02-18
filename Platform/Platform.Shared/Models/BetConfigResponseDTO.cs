using System.Collections.Generic;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Platform.Shared.Models
{
    public class BetConfigResponseDTO
    {
        public BetConfigResponseDTO()
        {
            OverrideSportConfig = new List<OverrideSportConfig>();
        }
        public DefaultSportConfig DefaultSportConfig { get; set; }
        public List<OverrideSportConfig> OverrideSportConfig { get; set; }
        public ParlayConfig Parlayconfig { get; set; }
    }

    public class OverrideSportConfig
    {
        [JsonProperty("sportId")] 
        public int SportId { get; set; }
        [JsonProperty("sportName")]
        public string SportName { get; set; }
        [JsonProperty("minStake")]
        public decimal MinStake { get; set; }
        [JsonProperty("maxStake")]
        public decimal MaxStake { get; set; }
        [JsonProperty("maxPerGame")]
        public decimal MaxPerGame { get; set; }
    }

    public class DefaultSportConfig
    {
        [JsonProperty("minStake")] 
        public decimal MinStake { get; set; }
        [JsonProperty("maxStake")]
        public decimal MaxStake { get; set; }
        [JsonProperty("maxPerGame")]
        public decimal MaxPerGame { get; set; }
    }

    public class ParlayConfig
    {
        [JsonProperty("minStake")] 
        public decimal MinStake { get; set; }
        [JsonProperty("maxStake")]
        public decimal MaxStake { get; set; }
        [JsonProperty("maxPayout")]
        public decimal MaxPayout { get; set; }
    }
}
