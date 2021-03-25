using System.Collections.Generic;

namespace Platform.API.Models
{
    public class PostClientTopFixtureRequest
    {
        public PostClientTopFixtureRequest()
        {
            TopFixtures = new List<ClientTopFixtureSetup>();
        }

        public int ClientId { get; set; }
        public List<ClientTopFixtureSetup> TopFixtures { get; set; }
    }

    public class ClientTopFixtureSetup
    {
        public long FixtureId { get; set; }
        public int SortOrder { get; set; }
    }
}
