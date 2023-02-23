namespace Platform.Operator.API.Enums
{
    public enum AuthStatusCode
    {
        Success = 100,
        Missing_Or_Unsupported_APIKey_In_Request_Headers = 101,
        Missing_Or_Unsupported_OperatorToken_In_Request_Headers = 103,
        Unauthorized = 401,
        Error = 1000
    }
}
