using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnBOBetManualSettlement
    {
        public OnBOBetManualSettlement()
        {
            Bets = new List<BetManualSettlement>();
        }
        public List<BetManualSettlement> Bets { get; set; }
    }

    public class BetManualSettlement
    {
        public Guid BetId { get; set; }
        public BetStatus NewStatus { get; set; }
        public string Reason { get; set; }
    }
}
