using System.Collections.Generic;

namespace Platform.API.Models
{
    public class AllClientTopFixtureRequest
    {
        public AllClientTopFixtureRequest()
        {
            ClientIds = new List<int>();
        }

        public List<int> ClientIds { get; set; }
    }
}
