namespace Platform.Shared.Enums
{
    public enum StatusCode
    {
        Success = 100,
        Missing_Or_Unsupported_APIKey_In_Request_Headers = 101,
        Missing_Or_Unsupported_ClientId_In_Request_Headers = 102,
        Missing_Or_Unsupported_OperatorToken_In_Request_Headers = 103,
        Missing_Or_Unsupported_CurrencyCode_In_Request_Headers = 104,

        Insufficient_Funds = 105,

        Stake_Must_Be_Equal_Or_Higher_To_Min_Stake = 106,
        Stake_Must_Be_Equal_Or_Higher_To_Max_Stake = 107,
        Maximum_Stake_On_Fixture_Reached = 108,
        Maximum_Payout_Must_Be_Equal_Or_Lower = 109,
        Betting_Restricted = 110,
        Sport_Betting_Restricted = 111,
        Segment_Betting_Restricted = 112,
        League_Betting_Restricted = 113,

        Error = 1000,
        Operator_Error = 1001
    }
}
