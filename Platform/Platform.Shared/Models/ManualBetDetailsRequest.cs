using Platform.Shared.Enums;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ManualBetDetailsRequest
    {
        public string LanguageCode { get; set; }
        public ManualBetDetailsRequest()
        {
            BetIds = new List<string>();
        }
        public List<string> BetIds { get; set; }
    }
}
