using Platform.Shared.Models;
using System;

namespace Platform.Shared.Utility
{
    public class ReportUtility
    {
        public static void ReportBet(IMessageBroker messageBroker, BetModelRequestDTO request)
        {
            messageBroker.Publish(request, "Bet", "r.platform.report", Guid.NewGuid().ToString());
        }

        public static void ReportSelection(IMessageBroker messageBroker, ReportBetSelectionDTO selection)
        {
            messageBroker.Publish(selection, "SelectionUpdate", "r.platform.report", Guid.NewGuid().ToString());
        }
    }
}
