using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientPostMarketTemplateGroupRequestDTO
    {
        public ClientPostMarketTemplateGroupRequestDTO()
        {
            MarketTemplateGroups = new List<ClientMarketTemplateGroupModel>();
        }
        public bool IsAdd { get; set; }
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public List<ClientMarketTemplateGroupModel> MarketTemplateGroups { get; set; }
    }
}
