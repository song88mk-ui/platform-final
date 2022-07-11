using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientMarketTemplateGroupResponseDTO2
    {
        public ClientMarketTemplateGroupResponseDTO2()
        {
            MarketTemplateGroups = new List<ClientMarketTemplateGroupDTO>();
        }
        public List<ClientMarketTemplateGroupDTO> MarketTemplateGroups { get; set; }
    }

    public class ClientMarketTemplateGroupDTO
    {
        public int FixtureStatusId { get; set; }
        public int SportId { get; set; }
        public string MarketTemplateIds { get; set; }
    }
}
