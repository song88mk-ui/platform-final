using Platform.Shared.Models;
using System.Collections.Generic;

namespace Platform.API.Models
{
    public class BetBuilderSelectionsOddsRequest
    {
        public BetBuilderSelectionsOddsRequest()
        {
            Selections = new List<BetSelectionOddsRequest>();
        }
        public List<BetSelectionOddsRequest> Selections { get; set; }
    }

}
