namespace Platform.Shared.Enums
{
    public enum StatusCode
    {
        Success = 100,
        Missing_Or_Unsupported_APIKey_In_Request_Headers = 101,
        Missing_Or_Unsupported_ClientId_In_Request_Headers = 102,
        Missing_Or_Unsupported_OperatorToken_In_Request_Headers = 103,

        Insufficient_Funds = 104,

        Unsupported_CurrencyCode = 105,
        Unsupported_LanguageCode = 106,
        Unsupported_OddsType = 107,
        Unsupported_Channel = 108,
        Unsupported_FixtureType = 109,
        Unsupported_FixtureStatus = 110,
        CurrencyCode_Mismatch = 111,
        Stake_Must_Be_Equal_Or_Higher_To_Min_Stake = 112,
        Stake_Must_Be_Equal_Or_Higher_To_Max_Stake = 113,
        Maximum_Stake_On_Fixture_Reached = 114,
        Maximum_Payout_Must_Be_Equal_Or_Lower = 115,
        Betting_Restricted = 116,
        Sport_Betting_Restricted = 117,
        Segment_Betting_Restricted = 118,
        League_Betting_Restricted = 119,

        Error = 1000,
        Operator_Error = 1001
    }
}
