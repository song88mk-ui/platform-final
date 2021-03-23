using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientTopFixtureResponseDTO
    {
        public ClientTopFixtureResponseDTO()
        {
            TopFixtures = new List<OrderedTopFixture>();
        }

        public List<OrderedTopFixture> TopFixtures { get; set; }
    }

    public class OrderedTopFixture : TopFixture
    {
        public Guid ClientId { get; set; }
        public int SortOrder { get; set; }
    }
}
