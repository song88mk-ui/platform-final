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

    public class ClientTopLeague
    {
        public int clientId { get; set; }
        public int SportId { get; set; }
        public List<TempTopLeague> TopLeagues { get; set; }
    }
}
