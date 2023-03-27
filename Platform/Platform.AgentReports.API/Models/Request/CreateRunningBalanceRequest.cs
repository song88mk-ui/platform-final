namespace Platform.AgentReports.API.Models.Request
{
    public class CreateRunningBalanceRequest
    {
        public long ParentAgentId { get; set; }
        public long UserId { get; set; }
        public string Username { get; set; }
        public int CurrencyCode { get; set; }
        public int UserType { get; set; }
        public int PlayerType { get; set; }
    }
}
