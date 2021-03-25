using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BackOfficeTopLeaguesResponseDTO
    {
        public BackOfficeTopLeaguesResponseDTO()
        {
            ClientTopLeagues = new List<ClientTopLeague>();
        }
        public List<ClientTopLeague> ClientTopLeagues { get; set; }
    }
}
