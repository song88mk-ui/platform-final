using Platform.Shared.Models;
using System;

namespace Platform.API.Models
{
    public class ClientResponse
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public bool IsActive { get; set; }
        public string Endpoint { get; set; }
        public decimal CompanyShare { get; set; }
        public int ClientIdRef { get; set; }
        public decimal TaxPercentStake { get; set; }
        public decimal TaxPercentWinnings { get; set; }
    }

    public class ForexEURResponse
    {
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
    }
}
