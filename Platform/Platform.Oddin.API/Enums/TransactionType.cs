namespace Platform.Oddin.API.Enums
{
    public enum TransactionType
    {
        DebitAfterCredit = -2,
        Block = -1,
        DebitWithUpdatedPossibleWin = -3,
        Refund = 1,
        Credit = 2,
        Void = 3,
        Lost = 4
    }
}
