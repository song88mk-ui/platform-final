namespace Platform.QTech.API.Enums
{
    public enum AuthenticationCode
    {
        OK = 200,
        VALIDATION_ERROR = 422,
        USER_BLOCKED = 403,
        LOGIN_FAILED = 401,
        UNKNOWN_ERROR = 500,
        QT_NOT_AVAILABLE = 503
    }
}
