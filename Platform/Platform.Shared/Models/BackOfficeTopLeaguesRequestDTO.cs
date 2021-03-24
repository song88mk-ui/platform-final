using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BackOfficeTopLeaguesRequestDTO
    {
        public BackOfficeTopLeaguesRequestDTO()
        {
            ClientIdRefs = new List<int>();
        }

        public List<int> ClientIdRefs { get; set; }
        public int SportId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
}
