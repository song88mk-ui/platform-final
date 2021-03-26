using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientTopFixtureRequestDTO
    {
        public ClientTopFixtureRequestDTO()
        {
            ClientIds = new List<Guid>();
        }

        public List<Guid> ClientIds { get; set; }
        public LanguageCode LanguageCode { get; set; } = LanguageCode.EN;
    }
}
