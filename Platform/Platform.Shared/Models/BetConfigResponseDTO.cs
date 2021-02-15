using System.Collections.Generic;

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
        public int SportId { get; set; }
        public string SportName { get; set; }
        public decimal MinStake { get; set; }
        public decimal MaxStake { get; set; }
    }

    public class DefaultSportConfig
    {
        public decimal MinStake { get; set; }
        public decimal MaxStake { get; set; }
    }

    public class ParlayConfig
    {
        public decimal MinStake { get; set; }
        public decimal MaxStake { get; set; }
    }
}
