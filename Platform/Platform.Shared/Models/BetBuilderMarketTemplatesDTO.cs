using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetBuilderMarketTemplatesDTO
    {
        public BetBuilderMarketTemplatesDTO()
        {
            BetBuilderMarketTemplates = new List<BetBuilderMarketTemplateDTO>();
        }
        public List<BetBuilderMarketTemplateDTO> BetBuilderMarketTemplates { get; set; }
    }

    public class BetBuilderMarketTemplateDTO
    {
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public bool Prematch { get; set; }
        public bool Live { get; set; }
    }

}
