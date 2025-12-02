namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class BetBuilderMarketTemplates
    {
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public short Prematch { get; set; }
        public short Live { get; set; }
    }
}
