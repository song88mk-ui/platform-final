using System;

namespace Platform.AgentReports.API.Models.Request
{
    public class ProfitLossRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public long? SelectedUserId { get; set; }
        public string Product { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string DateFilter { get; set; } = "transactiondate";
        public string Accounts { get; set; }
        public string BetIds { get; set; }
    }
}
