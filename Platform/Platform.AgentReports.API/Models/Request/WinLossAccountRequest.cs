using Platform.Shared.Enums;
using System;

namespace Platform.AgentReports.API.Models.Request
{
    public class WinLossAccountRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public CurrencyCode Currency { get; set; } = CurrencyCode.ALL;
        public int ProductTypeId { get; set; } = (int)Shared.Enums.Product.All;
        public string Product { get; set; }
        public string Status { get; set; }
        public string DateFilter { get; set; } = "transactiondate";
        public string Accounts { get; set; }
        public string BetIds { get; set; }
    }
}
