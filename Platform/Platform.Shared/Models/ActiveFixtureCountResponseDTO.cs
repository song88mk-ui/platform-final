using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ActiveFixtureCountResponseDTO
    {
        public ActiveFixtureCountResponseDTO()
        {
            Sports = new List<ActiveFixtureCount>();
        }
        public List<ActiveFixtureCount> Sports { get; set; }
    }
    public class ActiveFixtureCount
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int PrematchCount { get; set; }
        public int LiveCount { get; set; }
    }
}
