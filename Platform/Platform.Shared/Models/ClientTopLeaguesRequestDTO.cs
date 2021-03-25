using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientTopLeaguesRequestDTO
    {
        public ClientTopLeaguesRequestDTO()
        {
            ClientIds = new List<Guid>();
        }

        public List<Guid> ClientIds { get; set; }
        public int SportId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
}
