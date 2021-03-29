using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientTopFixtureResponseDTO
    {
        public ClientTopFixtureResponseDTO()
        {
            TopFixtures = new List<OrderedClientTopFixture>();
        }

        public List<OrderedClientTopFixture> TopFixtures { get; set; }
    }

    public class OrderedClientTopFixture
    {
        public Guid ClientId { get; set; }
        public int ClientIdRef { get; set; }
        public string ClientName { get; set; }
        public List<OrderedTopFixture> TopFixtures { get; set; }
    }

    public class OrderedTopFixture : TopFixture
    {
        public DateTime FixtureStartDate { get; set; }
        public string FixtureStatus { get; set; }
        public int SortOrder { get; set; }
    }
}
