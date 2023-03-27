namespace Platform.AgentReports.API.Models.Response
{
    public class ProfileResponse
    {
        public long AgentId { get; set; }
        public decimal CashBalance { get; set; }
        public decimal PlayersBalance { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int TotalOutstandingCount { get; set; }
        public decimal WLCToday { get; set; }
        public decimal WLCYesterday { get; set; }
        public decimal WLCLastWeek { get; set; }
    }
}

