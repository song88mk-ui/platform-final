namespace Platform.AgentReports.API.Models.Response
{
    public class WalletResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public decimal Balance { get; set; }
    }
}
