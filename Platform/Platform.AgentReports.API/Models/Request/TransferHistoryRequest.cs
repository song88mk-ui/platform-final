using System;

namespace Platform.AgentReports.API.Models.Request
{
    public class TransferHistoryRequest
    {
        public long AgentId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int UserTypeId { get; set; }
        public bool IsCashMarket { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Username { get; set; }
        public string CurrencyCode { get; set; }
    }
}
