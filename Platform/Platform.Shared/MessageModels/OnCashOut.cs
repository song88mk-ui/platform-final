using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnCashOut
    {
        public OnCashOut()
        {
            BetSelections = new List<BetSelection>();
        }
        public Bet Bet { get; set; }
        public List<BetSelection> BetSelections { get; set; }
    }
}
