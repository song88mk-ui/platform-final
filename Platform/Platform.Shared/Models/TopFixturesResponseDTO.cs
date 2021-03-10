using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class TopFixturesIdsResponseDTO
    {
        public TopFixturesIdsResponseDTO()
        {
            TopFixtures = new List<string>();
        }
        public List<string> TopFixtures { get; set; }
    }
    public class TopFixturesResponseDTO
    {
        public TopFixturesResponseDTO()
        {
            TopFixtures = new List<TopFixture>();
        }
        public List<TopFixture> TopFixtures { get; set; }
    }

    public class TopFixture
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureId { get; set; }
        public string FixtureName { get; set; }
    }

    public class TempTopFixture : TopFixture
    {
        public DateTime FixtureStartDate { get; set; }
        public int SortOrder { get; set; }
    }
}
