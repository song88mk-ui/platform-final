using Platform.Shared.Enums;

namespace Platform.AgentReports.API.Models.Request
{
    public class TotalBetsRequest
    {
        public long AgentId { get; set; }
        public string AgentPartnerUserName { get; set; }
        public CurrencyCode CurrencyCode { get; set; }
    }
}
