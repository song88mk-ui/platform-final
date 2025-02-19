using System.ComponentModel;

namespace Platform.RedCat.API.Enums
{
    public enum RedCatStatusCode
    {
        Success = 100,
        Insufficient_Funds = 104,

        Error = 1000,
        Operator_Error = 1001,

        // Error codes
        [Description("The supplied startDate is not ISO 8601")]
        StartDate_Not_ISO_8601 = 10001,

        [Description("The supplied endDate is not ISO 8601")]
        EndDate_Not_ISO_8601 = 10002,

        [Description("Unknown language code supplied")]
        Unknown_Language_Code = 10003,

        [Description("Unknown operator id supplied")]
        Unknown_Operator_Id = 10004,

        [Description("Unknown member code supplied")]
        Unknown_Member_Code = 10005,

        [Description("IsSettled must be true or false if supplied")]
        IsSettled_Invalid_Value = 10006,

        [Description("Unknown gameId supplied")]
        Unknown_GameId = 10007,

        [Description("Invalid security hash")]
        Invalid_Security_Hash = 10008,

        [Description("Date range too wide (maximum 7 day range allowed)")]
        Date_Range_Too_Wide = 10009,

        [Description("Page size not allowed (must be greater than 0 and equal or less than 1000)")]
        Invalid_Page_Size = 10010,

        [Description("Page number does not exist")]
        Page_Number_Does_Not_Exist = 10011,

        [Description("The supplied date is not valid")]
        Invalid_Supplied_Date = 10012,

        [Description("The BetId doesn’t exist")]
        BetId_Does_Not_Exist = 10013,

        [Description("Invalid MemberType")]
        Invalid_MemberType = 10014,

        [Description("Invalid operatorGroupId")]
        Invalid_Operator_GroupId = 10015
    }
}
