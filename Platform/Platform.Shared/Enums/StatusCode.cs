namespace Platform.Shared.Enums
{
    public enum StatusCode
    {
        Success = 100,
        Missing_Unsupported_ClientId_In_Request_Headers = 101,
        Missing_Unsupported_CurrencyCode_In_Request_Headers = 102,
        Missing_Unsupported_LanguageCode_In_Request_Headers = 103,
        Missing_Unsupported_OperatorToken_In_Request_Headers = 104,
        Missing_Unsupported_APIKey_In_Request_Headers = 1000
    }
}
