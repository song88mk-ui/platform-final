namespace Platform.Shared.Enums
{
    public enum StatusType
    {
        Waiting = 1000,
        Running = 2000,
        Won = 3000,
        HalfWon = 3001,
        FreeSpinWon = 3002,
        Lost = 4000,
        HalfLost = 4001,
        Void = 5000,
        Rejected = 6000,
        Cancelled = 7000
    }

    public enum FilterStatus
    {
        All = 0,
        Won = 1,
        Lost = 2,
        Void = 3,
        Cancelled = 4,
        Rejected = 5
    }
}
