namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class MarketTemplateGroup
    {
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string MarketTemplates { get; set; }
    }
}
