using System.Collections.Generic;

namespace Platform.Shared.Models
{
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
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
    }
}
