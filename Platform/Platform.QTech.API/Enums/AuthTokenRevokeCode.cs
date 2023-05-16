namespace Platform.QTech.API.Enums
{
    public enum AuthTokenRevokeCode
    {
        OK = 200,
        INVALID_TOKEN = 401,
        UNKNOWN_ERROR = 500,
        QT_NOT_AVAILABLE = 503,
        PROVIDER_ERROR = 1000
    }
}
