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
        public int ClientIdRef { get; set; }
        public int SportId { get; set; }
        public List<SportLeague> TopLeagues { get; set; }
    }
}
