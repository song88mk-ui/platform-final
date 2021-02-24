namespace Platform.Shared.Enums
{
    public enum BetFixtureStatus
    {
        Success = 0,
        MarketInactive = 1,
        OddsIsStale = 2,
        BetStopFixture = 3,
        BetStopGlobal = 4
    }

    public enum MarketStatus
    {
        NotMapped = 99,
        Cancelled = -4,
        Settled = -3,
        HandedOver = -2,
        Suspended = -1,
        Inactive = 0,
        Active = 1
    }
}
