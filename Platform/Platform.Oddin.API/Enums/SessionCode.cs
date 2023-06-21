namespace Platform.Oddin.API.Enums
{
    public enum SessionCode
    {
        OK = 0,
        InvalidToken = 1,
        UserAccount = 2,
        InvalidCurrency = 3,
        InsufficientFunds = 4,
        TransactionAlreadyProcessed = 5,
        OriginalTransactionNotFound = 6,
        LimitExceeded = 7,
        CustomerError = 8,
        OtherError = -1
    }
}
