using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientMarketTemplateGroupResponseDTO
    {
        public ClientMarketTemplateGroupResponseDTO()
        {
            ClientMarketTemplateGroups = new List<ClientMarketTemplateGroupModel>();
        }

        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public List<ClientMarketTemplateGroupModel> ClientMarketTemplateGroups { get; set; }
    }

    public class ClientMarketTemplateGroupModel
    {
        public int MarketTemplateGroupId { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string MarketTemplateIds { get; set; }
    }

    public class SelectedClientMarketGroupModel
    {
        public int FixtureStatusId { get; set; }
        public string MarketTemplateIds { get; set; }
    }
}
