using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class CopyClientTopFixtureRequestDTO
    {
        public CopyClientTopFixtureRequestDTO()
        {
            ClientIds = new List<Guid>();
            TopFixtures = new List<ClientTopFixtureSetup>();
        }

        public List<Guid> ClientIds { get; set; }
        public List<ClientTopFixtureSetup> TopFixtures { get; set; }
    }
}
