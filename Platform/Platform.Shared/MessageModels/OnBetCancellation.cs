using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnBetCancellation
    {
        public Bet Bet { get; set; }
        public List<BetSelection> BetSelections { get; set; }
        public bool IsSuccess { get; set; }
    }
}
