namespace Platform.Operator.API.Enums
{
    public enum AuthStatusCode
    {
        Success = 100,
        Missing_Or_Unsupported_APIKey_In_Request_Headers = 101,
        Missing_Or_Unsupported_OperatorToken_In_Request_Headers = 103,
        Not_Authorized = 140,
        Invalid_Password = 201,
        Username_Not_Found = 202,
        Record_Is_Already_Existing = 203,
        User_Is_Locked = 204,
        Unauthorized = 401,
        Error = 1000
    }
}
