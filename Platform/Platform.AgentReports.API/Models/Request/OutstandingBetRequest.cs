namespace Platform.AgentReports.API.Models.Request
{
    public class OutstandingBetRequest
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long ParentId { get; set; }
        public long AgentId { get; set; }
    }
}
