using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class ClientMarketTemplateGroup
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string MarketTemplates { get; set; }
    }
}
