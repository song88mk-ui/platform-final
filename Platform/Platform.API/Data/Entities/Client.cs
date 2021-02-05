using System;

namespace Platform.API.Data.Entities
{
    public class Client
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public bool IsActive { get; set; }
        public string Endpoint { get; set; }
    }
}
