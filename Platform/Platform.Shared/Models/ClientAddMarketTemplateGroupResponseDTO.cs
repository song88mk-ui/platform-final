using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientAddMarketTemplateGroupResponseDTO
    {
        public ClientAddMarketTemplateGroupResponseDTO()
        {
            FixtureStatusIds = new List<int>();
        }

        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public List<int> FixtureStatusIds { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string MarketTemplateIds { get; set; }
    }
}
