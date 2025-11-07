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
        [JsonProperty("stakeLimitFixture")]
        public decimal StakeLimitFixture { get; set; }
    }

    public class DefaultSportConfig
    {
        [JsonProperty("minStake")]
        public decimal MinStake { get; set; }
        [JsonProperty("maxStake")]
        public decimal MaxStake { get; set; }
        [JsonProperty("stakeLimitFixture")]
        public decimal StakeLimitFixture { get; set; }
    }

    public class ParlayConfig
    {
        public ParlayConfig()
        {
            ParlayBonusConfig = new List<ParlayBonusConfig>();
        }
        [JsonProperty("minStake")]
        public decimal MinStake { get; set; }
        [JsonProperty("maxStake")]
        public decimal MaxStake { get; set; }
        [JsonProperty("maxPayout")]
        public decimal MaxPayout { get; set; }
        [JsonProperty("parlayBonusMaxWinnings")]
        public decimal ParlayBonusMaxWinnings { get; set; }
        [JsonProperty("parlayBonusMinOdds")]
        public decimal ParlayBonusMinOdds { get; set; }
        [JsonProperty("parlayBonusConfig")]
        public List<ParlayBonusConfig> ParlayBonusConfig { get; set; }
    }

}
