using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ActiveFixturesResponseDTO
    {
        public ActiveFixturesResponseDTO()
        {
            Fixtures = new List<ActiveFixture>();
        }
        public List<ActiveFixture> Fixtures { get; set; }
    }

    public class ActiveFixture
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureType { get; set; }
        public string FixtureId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string FixtureName { get; set; }
        public string Home { get; set; }
        public string Away { get; set; }
        public string FixtureStatus { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public bool IsLive { get; set; }
        public bool IsLiveStream { get; set; }
    }
}
