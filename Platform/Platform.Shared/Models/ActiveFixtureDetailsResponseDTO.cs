using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ActiveFixtureDetailsResponseDTO
    {
        public ActiveFixtureDetailsResponseDTO()
        {
            Fixtures = new List<FixtureLimitedDTO>();
        }
        public List<FixtureLimitedDTO> Fixtures { get; set; }
    }

}
