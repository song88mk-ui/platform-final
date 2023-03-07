namespace Platform.AgentReports.API.Models.Response
{
    public class MonthlyGrossGamingRevenueResponse
    {
        public string Month { get; set; }
        public decimal CompanyShare { get; set; }
        public decimal GGR { get; set; }
        public decimal CompanyGGR { get; set; }
        public decimal MonthlyFee { get; set; }
        public decimal SettlementAmount { get; set; }
    }
}
