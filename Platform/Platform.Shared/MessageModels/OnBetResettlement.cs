using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnBetResettlement
    {
        public OnBetResettlement()
        {
            BetSelections = new List<BetSelection>();
        }    
        public Bet Bet { get; set; }
        public List<BetSelection> BetSelections { get; set; }
    }
}
