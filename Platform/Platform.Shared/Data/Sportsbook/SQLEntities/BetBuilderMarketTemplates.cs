namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class BetBuilderMarketTemplates
    {
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public bool Prematch { get; set; }
        public bool Live { get; set; }
    }
}
