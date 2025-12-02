namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class BetBuilderMarketTemplates
    {
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public int Prematch { get; set; }
        public int Live { get; set; }
    }
}
