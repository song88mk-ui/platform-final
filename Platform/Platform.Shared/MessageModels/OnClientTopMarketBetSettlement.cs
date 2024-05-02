namespace Platform.Shared.MessageModels
{
    public class OnClientTopMarketBetSettlement
    {
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public long MarketId { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
    }
}
