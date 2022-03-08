using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ManualBetDetailsRequest
    {
        public ManualBetDetailsRequest()
        {
            BetIds = new List<string>();
        }
        public List<string> BetIds { get; set; }
    }
}
