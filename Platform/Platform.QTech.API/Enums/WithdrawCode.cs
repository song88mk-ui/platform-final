namespace Platform.QTech.API.Enums
{
    public enum WithdrawCode
    {
        OK = 200,
        REQUEST_DECLINED = 400,
        INVALID_TOKEN = 400,
        INSUFFICIENT_FUNDS = 400,
        LIMIT_EXCEEDED = 400,
        LOGIN_FAILED = 401,
        ACCOUNT_BLOCKED = 403,
        UNKNOWN_ERROR = 500
    }
}
