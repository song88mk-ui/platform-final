using System;

namespace Platform.Shared.Data.Platform.Entities
{
    public class Client
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public bool IsActive { get; set; }
        public string Endpoint { get; set; }
        public decimal CurrentCompanyShare { get; set; }
        public DateTime EffectiveDate { get; set; }
        public decimal NextCompanyShare { get; set; }
        public int ClientIdRef { get; set; }
    }
}
