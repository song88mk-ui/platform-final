using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientTopLeaguesResponseDTO
    {
        public ClientTopLeaguesResponseDTO()
        {
            ClientTopLeagues = new List<ClientTopLeague>();
        }
        public List<ClientTopLeague> ClientTopLeagues { get; set; }
    }

    public class ClientTopLeague
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public List<SportLeague> TopLeagues { get; set; }
    }
}
