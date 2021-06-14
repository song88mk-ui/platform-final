using Platform.Shared.Data.Platform.Entities;
using System;

namespace Platform.Shared.Models
{
    public class ClientRequestDTO
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public bool IsActive { get; set; }
        public string Endpoint { get; set; }
        public decimal MonthlyFee { get; set; }
        public decimal CurrentCompanyShare { get; set; }
        public DateTime EffectiveDate { get; set; }
        public decimal NextCompanyShare { get; set; }
        public int ClientIdRef { get; set; }
        public decimal TaxPercentStake { get; set; }
        public decimal TaxPercentWinnings { get; set; }
    }

}
