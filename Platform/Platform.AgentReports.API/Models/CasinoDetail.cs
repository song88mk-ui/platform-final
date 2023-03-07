using System;

namespace Platform.AgentReports.API.Models
{
    public class CasinoDetail
    {
        public string ProviderTransactionId { get; set; }
        public DateTime ProviderTransactionDate { get; set; }
        public int ProviderId { get; set; }
        public string ProviderName { get; set; }
        public int ProviderBrandId { get; set; }
        public string GameTransactionId { get; set; }
        public string ProviderBrandName { get; set; }
        public int GameId { get; set; }
        public string GameName { get; set; }
        public int GameTypeId { get; set; }
        public string GameTypeName { get; set; }
        public decimal Payout { get; set; }
    }
}
