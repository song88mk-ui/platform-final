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
        Odds_Must_Be_Greater_Or_Higher_To_1 = 112,
        Stake_Must_Be_Greater_Than_Zero = 113,
        Stake_Must_Be_Higher_Or_Equal_To_Min_Stake = 114, //Stake_Must_Be_Equal_Or_Higher_To_Min_Stake
        Stake_Must_Be_Lower_Or_Equal_To_Max_Stake = 115, //Stake_Must_Be_Equal_Or_Lower_To_Max_Stake
        Maximum_Bet_Amount_For_The_Game_Has_Been_Reached = 116, //Maximum_Bet_Limit_On_Fixture_Reached
        Maximum_Payout_Must_Be_Equal_Or_Lower = 117,
        Betting_Restricted = 118,
        Sport_Betting_Restricted = 119,
        Segment_Betting_Restricted = 120,
        League_Betting_Restricted = 121,

        IPAddress_Must_Be_IPv4 = 122,
        Fixture_Has_No_Livestream = 123,

        Market_Is_Suspended = 124,
        Market_Is_Not_Offered = 125, //Market_Is_Inactive
        Odds_Or_Lines_Have_Changed = 126, //Odds_Is_Stale

        Odds_Is_Inactive = 128,
        Market_Has_Been_Suspended = 129, //Betstop_On_Fixture
        Betstop_On_Global = 130,

        Max_Liability_By_Tier_Not_Found = 131,
        Mts_Error = 132,

        Search_Text_Needs_To_Be_3_Characters_Or_Above = 133,

        Cannot_Have_Duplicate_Top_Fixture_Entries = 134,
        Cannot_Have_Duplicate_Top_League_Entries = 135,
        Cannot_Have_Duplicate_Sort_Order_Entries = 136,
        Top_Fixture_Entries_Must_Be_Equal_Or_Lower_Than_50 = 137,
        Top_League_Entries_Must_Be_Equal_Or_Lower_Than_50 = 138,

        //Bet_Status_Must_Be_Running = 139,
        //Cashout_Must_Be_Less_Than_Potential_Payout = 140,
        //Cashout_Must_Be_Greater_Than_Stake = 141,

        No_Record_Found = 139,

        Not_Authorized = 140,

        CashOut_Is_Unavailable = 141,
        CashOut_Stake_Changed_From_X_To_Y = 142,
        Bet_Not_Found = 143,

        Parlay_Bonus_Not_Updated = 144,

        Virtuals_Cannot_Be_Added_As_Part_Of_Parlay_With_Other_Sports = 145,

        Livestream_Is_Not_Available_In_Your_Region = 146,

        Placing_Multiple_Bets_On_The_Same_Boosted_Odds_Is_Not_Allowed = 147,
        Boosted_Odds_Cannot_Be_Parlayed = 148,
        Boosted_Odds_Not_Allowed_Once_Fixture_Transitions_To_Live = 149,

        Rejected_Bet_Are_Not_Allowed_To_Be_Resent_To_Operator = 150,
        Resend_Attempt_Not_Successful_Please_Try_Again = 151,

        FromDate_Must_Be_Earlier_Than_ToDate = 152,
        Maximum_Of_24Hrs_Per_Request = 153,

        Bet_Builder_Must_Have_2_Or_More_Selections = 154,
        Bet_Builder_Cannot_Be_More_Than_10_Selections = 155,

        Boosted_Odds_Cannot_Be_Combined_With_Bet_Builder = 156,
        Bet_Builder_Must_Be_On_The_Same_Fixture = 157,

        Total_Odds_Must_Be_Greater_Or_Higher_To_1 = 158,
        Bet_Builder_Selections_Cannot_Be_Combined = 159,

        Boosted_Odds_Is_Only_Applicable_With_Decimal_Odds_Type_Format = 160,

        Error = 1000,
        Operator_Error = 1001,

        Invalid_Password = 201,
        Username_Not_Found = 202,

        Record_Is_Already_Existing = 203,
        User_Is_Locked = 204
    }
}
