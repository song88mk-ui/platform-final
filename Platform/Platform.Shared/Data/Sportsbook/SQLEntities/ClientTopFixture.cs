using System;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    public class ClientTopFixture
    {
        public Guid ClientId { get; set; }
        public long FixtureId { get; set; }
        public int SortOrder { get; set; }
    }
}
