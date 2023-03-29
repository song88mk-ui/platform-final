namespace Platform.AgentReports.API.Models.Request
{
    public class GGRSettlementRequest
    {
        public long UserId { get; set; }
        public decimal SettlementAmount { get; set; }
        public long SettledByUserId { get; set; }
        public string SettledByUsername { get; set; }
    }
}
