using Platform.Shared.Data.Betting.Entities;
using System;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnBetPlacement
    {
        public OnBetPlacement()
        {
            BetSelections = new List<BetSelection>();
        }
        public Guid BetId { get; set; } //remove once updated in placement consumer
        public Bet Bet { get; set; }
        public List<BetSelection> BetSelections { get; set; }
    }
}
