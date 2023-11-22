namespace Platform.Shared.MessageModels
{
    public class OnMarketDeactivationMesssage
    {
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public string FixtureSpecifier { get; set; }
        public long MarketId { get; set; }
    }
}
