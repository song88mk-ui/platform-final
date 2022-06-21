using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientMarketTemplateResponseDTO
    {
        public ClientMarketTemplateResponseDTO()
        {
            MarketTemplates = new List<ClientMarketTemplateModel>();
        }

        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public List<ClientMarketTemplateModel> MarketTemplates { get; set; }
    }

    public class ClientMarketTemplateModel
    {
        public ClientMarketTemplateModel()
        {
            MarketTemplateGroupNames = new List<string>();
        }

        public List<string> MarketTemplateGroupNames { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketTemplateName { get; set; }
        public int SortOrder { get; set; }
    }
}
