using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Utility
{
    public class ReportUtility
    {
        public static void ReportBet(IMessageBroker messageBroker, Bet bet, List<BetSelection> selection)
        {
            BetModelRequestDTO request = new BetModelRequestDTO { 
                Bet = bet, 
                BetSelections = selection 
            };
            messageBroker.Publish(request, "Bet.New", "r.platform.report", Guid.NewGuid().ToString());
        }

        public static void ReportSelection(IMessageBroker messageBroker, BetSelection selection)
        {
            messageBroker.Publish(selection, "SelectionUpdate.New", "r.platform.report", Guid.NewGuid().ToString());
        }        
    }
}
