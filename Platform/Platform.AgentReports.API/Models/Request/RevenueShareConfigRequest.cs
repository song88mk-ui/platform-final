namespace Platform.AgentReports.API.Models.Request
{
    public class RevenueShareConfigRequest
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public bool IsGGREnabled { get; set; }
        public decimal ClientGGR { get; set; }
        public decimal CompanyGGR { get; set; }
        public decimal MonthlyFee { get; set; }
        public long CreatedBy { get; set; }
    }
}
