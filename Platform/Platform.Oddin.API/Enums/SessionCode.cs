namespace Platform.Oddin.API.Enums
{
    public enum SessionCode
    {
        OK = 0,
        INVALID_TOKEN = 1,
        USER_ACCOUNT = 2,
        INVALID_CURRENCY = 3,
        INSUFFICIENT_FUNDS = 4,
        TRANSACTION_ALREADY_PROCESSED = 5,
        ORIGINAL_TRANSACTION_NOT_FOUND = 6,
        LIMIT_EXCEEDED = 7,
        CUSTOMER_ERROR = 8,
        OTHER_ERROR = -1
    }
}
