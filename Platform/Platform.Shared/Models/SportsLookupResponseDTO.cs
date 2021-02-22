using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class SportsLookupResponseDTO
    {
        public SportsLookupResponseDTO()
        {
            Sports = new List<SportLookup>();
        }
        public List<SportLookup> Sports { get; set; }
    }

    public class SportLookup
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
    }

    public class SegmentsLookupResponseDTO
    {
        public SegmentsLookupResponseDTO()
        {
            Segments = new List<SegmentLookup>();
        }
        public List<SegmentLookup> Segments { get; set; }
    }

    public class SegmentLookup
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
    }

    public class LeaguesLookupResponseDTO
    {
        public LeaguesLookupResponseDTO()
        {
            Leagues = new List<LeagueLookup>();
        }
        public List<LeagueLookup> Leagues { get; set; }
    }

    public class LeagueLookup
    {
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
    }
}
