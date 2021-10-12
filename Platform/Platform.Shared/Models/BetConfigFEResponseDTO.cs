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
        public TaxConfig TaxConfig { get; set; }
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
        public ParlayConfigFE()
        {
            ParlayBonusConfig = new List<ParlayBonusConfigFE>();
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
        public List<ParlayBonusConfigFE> ParlayBonusConfig { get; set; }
    }
    public class ParlayBonusConfigFE
    {
        [JsonProperty("selectionCount")]
        public int SelectionCount { get; set; }
        [JsonProperty("parlayBonus")]
        public decimal ParlayBonus { get; set; }
    }

    public class TaxConfig
    {
        [JsonProperty("stakeTax")]
        public decimal StakeTax { get; set; }
        [JsonProperty("winningsTax")]
        public decimal WinningsTax { get; set; }
    }
}
