using System;

namespace Platform.Shared.Models
{
    public class CasinoDetailDTO
    {
        public string ProviderTransactionId { get; set; }
        public DateTime ProviderTransactionDate { get; set; }
        public int ProviderId { get; set; } = 0;
        public string ProviderName { get; set; }
        public int ProviderBrandId { get; set; } = 0;
        public string GameTransactionId { get; set; }
        public string ProviderBrandName { get; set; }
        public int GameId { get; set; } = 0;
        public string GameName { get; set; }
        public string GameTypeId { get; set; }
        public string GameTypeName { get; set; }
        public decimal Payout { get; set; } = 0m;
    }
}
