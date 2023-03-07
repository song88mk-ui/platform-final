using Platform.AgentReports.Service.Enums;

namespace Platform.AgentReports.Service.Models
{
    public class AgentSettings
    {
        public long AgentId { get; set; }

        public string AgentUsername { get; set; }

        public string Username { get; set; }

        public decimal PositionTaking { get; set; }

        public decimal Commission { get; set; }

        public UserType UserType { get; set; }

        public long UplineUserId { get; set; }

        public bool IsCashMarket { get; set; }

        public string CurrencyCode { get; set; }

        public int Hierarchy { get; set; }
    }
}
