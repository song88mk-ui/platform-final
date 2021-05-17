using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnCashoutBetSettlement
    {
        public Bet Bet { get; set; }
        public List<BetSelection> BetSelections { get; set; }
    }
}
