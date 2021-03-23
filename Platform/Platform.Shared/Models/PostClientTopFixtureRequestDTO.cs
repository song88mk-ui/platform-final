using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class PostClientTopFixtureRequestDTO
    {
        public PostClientTopFixtureRequestDTO()
        {
            TopFixtures = new List<ClientTopFixtureSetup>();
        }

        public Guid ClientId { get; set; }
        public List<ClientTopFixtureSetup> TopFixtures { get; set; }
    }

    public class ClientTopFixtureSetup
    {
        public long FixtureId { get; set; }
        public int SortOrder { get; set; }
    }
}
