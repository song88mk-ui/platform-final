namespace Platform.AgentReports.Service.Models
{
    public class AgentSettingsResponse
    {
        public long AgentId { get; set; }

        public string AgentUsername { get; set; }

        public string Username { get; set; }

        public decimal PositionTaking { get; set; }

        public decimal Commission { get; set; }

        public int UserType { get; set; }

        public long UplineUserId { get; set; }

        public bool IsCashMarket { get; set; }

        public string CurrencyCode { get; set; }

        public int Hierarchy { get; set; }
    }
}
