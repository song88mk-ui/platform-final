using System;

namespace Platform.AgentReports.API.Models.Request
{
    public class PartnerRevenueSettlementHistoryRequest
    {
        public string UserIds { get; set; }
        public string SettledBy { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
