using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientTopFixtureRequestDTO2
    {
        public ClientTopFixtureRequestDTO2()
        {
            ClientInfos = new List<ClientInfo>();
        }

        public List<ClientInfo> ClientInfos { get; set; }
        public LanguageCode LanguageCode { get; set; } = LanguageCode.EN;
    }

    public class ClientInfo
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
    }
}
