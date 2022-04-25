using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BackOfficeCopyClientTopLeague
    {
        public BackOfficeCopyClientTopLeague()
        {
            ClientIdRefs = new List<int>();
            TopLeagues = new List<SportLeague>();
        }
        
        public List<int> ClientIdRefs { get; set; }
        public int SportId { get; set; }
        public List<SportLeague> TopLeagues { get; set; }
    }
}
