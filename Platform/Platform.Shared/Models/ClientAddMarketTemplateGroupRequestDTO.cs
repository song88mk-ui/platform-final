using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientAddMarketTemplateGroupRequestDTO
    {
        public ClientAddMarketTemplateGroupRequestDTO()
        {
            FixtureStatusIds = new List<int>();
            MarketTemplateIds = new List<string>();
            MarketTemplates = new List<ClientMarketTemplateModel>();
        }

        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public List<int> FixtureStatusIds { get; set; }
        public string Name { get; set; }
        public List<string> MarketTemplateIds { get; set; }
        public List<ClientMarketTemplateModel> MarketTemplates { get; set; }
    }
}
