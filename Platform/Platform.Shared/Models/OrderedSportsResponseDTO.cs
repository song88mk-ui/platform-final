using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class OrderedSportsResponseDTO
    {
        public OrderedSportsResponseDTO()
        {
            Sports = new List<OrderedSport>();
        }
        public List<OrderedSport> Sports { get; set; }
    }

    public class OrderedSport
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public string Scope { get; set; }
        public int FixtureCount { get; set; }
    }

    public class TempOrderedSport : OrderedSport
    {
        public bool HasOutrights { get; set; }

        public int SortOrder { get; set; }
    }

    public class SportsResponseDTO
    {
        public SportsResponseDTO()
        {
            Sports = new List<SportDTO>();
        }
        public List<SportDTO> Sports { get; set; }
    }

    public class SportDTO
    {
        public SportDTO()
        {
            Segments = new List<SegmentDTO>();
        }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public string Scope { get; set; }
        public int FixtureCount { get; set; }
        public List<SegmentDTO> Segments { get; set; }
    }

    public class SegmentDTO
    {
        public SegmentDTO()
        {
            Leagues = new List<LeagueDTO>();
        }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int SortOrder { get; set; }
        public int CountryId { get; set; }
        public List<LeagueDTO> Leagues { get; set; }
    }

    public class LeagueDTO
    {
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
    }
}
