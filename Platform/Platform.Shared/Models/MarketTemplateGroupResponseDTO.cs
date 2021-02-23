using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class MarketTemplateGroupResponseDTO
    {
        public MarketTemplateGroupResponseDTO()
        {
            Sports = new List<SportMarketTemplateGroup>();
        }
        public List<SportMarketTemplateGroup> Sports { get; set; }
    }

    public class SportMarketTemplateGroup
    {
        public SportMarketTemplateGroup()
        {
            Prematch = new List<MarketTemplateGroup>();
            Live = new List<MarketTemplateGroup>();
        }
        public int SportId { get; set; }
        public List<MarketTemplateGroup> Prematch { get; set; }
        public List<MarketTemplateGroup> Live { get; set; }
    }

    public class MarketTemplateGroup
    {
        public int MarketTemplateGroupId { get; set; }
        public string MarketTemplateGroupName { get; set; }
        public int SortOrder { get; set; }
        public List<string> MarketTemplateIds { get; set; }
    }
}
