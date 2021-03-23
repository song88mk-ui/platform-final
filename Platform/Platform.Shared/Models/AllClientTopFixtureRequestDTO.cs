using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class AllClientTopFixtureRequestDTO
    {
        public AllClientTopFixtureRequestDTO()
        {
            ClientIds = new List<Guid>();
        }

        public List<Guid> ClientIds { get; set; }
        public LanguageCode LanguageCode { get; set; } = LanguageCode.EN;
    }
}
