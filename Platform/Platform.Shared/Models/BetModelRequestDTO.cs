using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetModelRequestDTO
    {
        public BetModelRequestDTO()
        {
            BetSelections = new List<BetSelection>();
        }
        public Bet Bet;
        public IEnumerable<BetSelection> BetSelections { get; set; }
    }
}
