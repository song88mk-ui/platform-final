using System;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnBOBetCancellaton
    {
        public OnBOBetCancellaton()
        {
            Bets = new List<BetCancel>();
        }
        public List<BetCancel> Bets { get; set; }
    }

    public class BetCancel
    {
        public Guid BetId { get; set; }
        public string Reason { get; set; }
    }
}
