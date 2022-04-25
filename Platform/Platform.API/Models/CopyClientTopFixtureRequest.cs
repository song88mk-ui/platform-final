using System.Collections.Generic;

namespace Platform.API.Models
{
    public class CopyClientTopFixtureRequest
    {
        public CopyClientTopFixtureRequest()
        {
            ClientIds = new List<int>();
            TopFixtures = new List<ClientTopFixtureSetup>();
        }

        public List<int> ClientIds { get; set; }
        public List<ClientTopFixtureSetup> TopFixtures { get; set; }
    }
}
