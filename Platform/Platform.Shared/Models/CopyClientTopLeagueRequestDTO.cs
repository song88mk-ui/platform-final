using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class CopyClientTopLeagueRequestDTO
    {
        public CopyClientTopLeagueRequestDTO()
        {
            ClientIds = new List<Guid>();
            TopLeagues = new List<SportLeague>();
        }

        public List<Guid> ClientIds { get; set; }
        public int SportId { get; set; }
        public List<SportLeague> TopLeagues { get; set; }
    }
}
