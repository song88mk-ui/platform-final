using System;

namespace Platform.Shared.Models
{
    public class BetConfigRequestDTO
    {
        public string ClientId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal CompanyShare { get; set; }
    }
}
