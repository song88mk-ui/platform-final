using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientPostMarketTemplateGroupResponseDTO
    {
        public ClientPostMarketTemplateGroupResponseDTO()
        {
            MarketTemplateGroups = new List<ClientMarketTemplateGroupModel>();
        }

        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public List<ClientMarketTemplateGroupModel> MarketTemplateGroups { get; set; }
    }
}
