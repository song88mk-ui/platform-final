using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientMarketTemplateRequestDTO
    {
        public ClientMarketTemplateRequestDTO()
        {
            MarketTemplates = new List<ClientMarketTemplateModel>();
        }

        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public List<ClientMarketTemplateModel> MarketTemplates { get; set; }
    }
}
