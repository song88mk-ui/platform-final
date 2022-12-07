using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class ClientSelectorMarketTemplate
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketTemplateCode { get; set; }
        public int SortOrder { get; set; }
        public string Identifier { get; set; }
    }
}
