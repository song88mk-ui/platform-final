using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetModelRequestDTO
    {
        public Bet Bet { get; set; }
        public IEnumerable<BetSelection> BetSelections { get; set; }
    }
}
