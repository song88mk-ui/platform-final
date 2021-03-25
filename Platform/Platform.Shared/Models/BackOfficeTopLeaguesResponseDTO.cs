using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BackOfficeTopLeaguesResponseDTO
    {
        public BackOfficeTopLeaguesResponseDTO()
        {
            ClientTopLeagues = new List<BackOfficeClientTopLeague>();
        }
        public List<BackOfficeClientTopLeague> ClientTopLeagues { get; set; }
    }

    public class BackOfficeClientTopLeague
    {
        public int ClientId { get; set; }
        public List<SportLeague> TopLeagues { get; set; }
    }
}
