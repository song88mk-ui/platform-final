namespace Platform.AgentReports.API.Models.Request
{
    public class PaymentTransferListRequest
    {
        public long ParentAgentId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int UserTypeId { get; set; }
        public bool IsCashMarket { get; set; }
        public string Username { get; set; }
        public string CurrencyCode { get; set; }
    }
}
