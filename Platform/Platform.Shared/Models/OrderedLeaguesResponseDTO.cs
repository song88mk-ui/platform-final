using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class OrderedLeaguesResponseDTO
    {
        public OrderedLeaguesResponseDTO()
        {
            Leagues = new List<OrderedLeague>();
        }
        public List<OrderedLeague> Leagues { get; set; }
    }

    public class OrderedLeague
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
    }
}
