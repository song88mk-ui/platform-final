namespace Platform.Shared.Enums
{
    public enum BetStatus
    {
        Waiting = 1000,
        Running = 2000,
        Won = 3000,
        HalfWon = 3001,
        Lost = 4000,
        HalfLost = 4001,
        Void = 5000,
        Rejected = 6000,
        Cancelled = 7000,
        CashedOut = 8000
    }
}
