using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class ClientMarketTemplateOrder
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public int SortOrder { get; set; }
    }
}
