using Platform.Shared.Enums;
using System;

namespace Platform.ClientSportsbook.API.Models
{
    public class ClientKeyFilter
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public string IpCountryCode { get; set; }
        public string IpAddress { get; set; }
    }
}
