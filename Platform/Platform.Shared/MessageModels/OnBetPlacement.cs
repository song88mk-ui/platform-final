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
        public Bet Bet { get; set; }
        public bool IsBoosted { get; set; }
        public bool IsBetBuilder { get; set; }
        public string BonusId { get; set; }
        public decimal Odds { get; set; }
        public string UserAgent { get; set; } = string.Empty;
        public bool IsOGClient { get; set; }
        public List<BetSelection> BetSelections { get; set; }
    }

    public class OnBetPlacementError
    {
        public string BetId { get; set; }
        public string Error { get; set; }
        public string StackTrace { get; set; }
        public string InnerException { get; set; }
    }
}
