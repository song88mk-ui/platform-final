using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Models;
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
        public decimal Odds { get; set; }
        public List<BetSelection> BetSelections { get; set; }
    }

    public class OnBetPlacementDTO
    {
        public OnBetPlacementDTO()
        {
            Selections = new List<BetSelectionRequestDTO>();
        }
        public Bet Bet { get; set; }
        public bool IsBoosted { get; set; }
        public List<BetSelectionRequestDTO> Selections { get; set; }
    }

    public class OnBetPlacementError
    {
        public string BetId { get; set; }
        public string Error { get; set; }
        public string StackTrace { get; set; }
        public string InnerException { get; set; }
    }
}
