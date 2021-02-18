using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class TopLeaguesResponseDTO
    {
        public TopLeaguesResponseDTO()
        {
            TopLeagues = new List<TopLeague>();
        }
        public List<TopLeague> TopLeagues { get; set; }
    }

    public class TopLeague
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
    }
}
