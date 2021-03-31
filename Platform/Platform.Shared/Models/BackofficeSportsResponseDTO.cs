using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BackofficeSportsResponseDTO
    {
        public BackofficeSportsResponseDTO()
        {
            Sports = new List<BackofficeSportDTO>();
        }
        public List<BackofficeSportDTO> Sports { get; set; }
    }

    public class BackofficeSportDTO
    {
        public BackofficeSportDTO()
        {
            Segments = new List<BackofficeSegmentDTO>();
        }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public List<BackofficeSegmentDTO> Segments { get; set; }
    }

    public class BackofficeSegmentDTO
    {
        public BackofficeSegmentDTO()
        {
            Leagues = new List<BackofficeLeagueDTO>();
        }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int SortOrder { get; set; }
        public List<BackofficeLeagueDTO> Leagues { get; set; }
    }

    public class BackofficeLeagueDTO
    {
        public BackofficeLeagueDTO()
        {
            Fixtures = new List<BackofficeFixtureDTO>();
        }

        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SortOrder { get; set; }
        public List<BackofficeFixtureDTO> Fixtures { get; set; }
    }

    public class BackofficeFixtureDTO
    {
        public string FixtureId { get; set; }
        public string FixtureName { get; set; }
        public DateTime StartDate { get; set; }
        public int SortOrder { get; set; }
    }

    public class OrderedFixture
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
    }

    public class TempOrderedFixture : OrderedFixture
    {
        public DateTime StartDate { get; set; }
        public int SortOrder { get; set; }
    }

    public class TempDistinctOrderedSport : OrderedSport, IEquatable<TempDistinctOrderedSport>
    {
        public bool HasOutrights { get; set; }
        public int SortOrder { get; set; }

        public bool Equals(TempDistinctOrderedSport other)
        {
            if (SportId == other.SportId && SportName == other.SportName && HasOutrights == other.HasOutrights && SortOrder == other.SortOrder)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return (SportId + SportName + HasOutrights + SortOrder).GetHashCode();
        }
    }
}
