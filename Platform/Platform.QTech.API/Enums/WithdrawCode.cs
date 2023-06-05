namespace Platform.QTech.API.Enums
{
    public enum WithdrawCode
    {
        OK = 200,
        REQUEST_DECLINED,
        INVALID_TOKEN,
        INSUFFICIENT_FUNDS,
        LIMIT_EXCEEDED,
        LOGIN_FAILED = 401,
        ACCOUNT_BLOCKED = 403,
        UNKNOWN_ERROR = 500
    }
}
