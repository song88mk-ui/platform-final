namespace Platform.Shared.Enums
{
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
