using Platform.Shared.Enums;
using System;

namespace Platform.AgentReports.API.Models.Request
{
    public class SearchBetRequest
    {
        public long AgentId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string ClientIds { get; set; }
        public string UserName { get; set; }
        public string AgentPartnerUserName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public CurrencyCode CurrencyCode { get; set; }
        public string BetId { get; set; }
    }
}
