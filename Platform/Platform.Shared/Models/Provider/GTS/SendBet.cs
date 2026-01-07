using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.MessageModels;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models.Provider.GTS
{
    public class SendBet
    {
        public Bet Bet { get; set; }
        public List<BetSelection> BetSelections { get; set; }
    }
}
