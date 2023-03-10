namespace Platform.AgentReports.API.Models.Request
{
    public class PartnerRevenueSettlementRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string Username { get; set; }
        public string CurrencyCode { get; set; }
    }
}
