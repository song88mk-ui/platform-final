using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
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
        public static Client GetClient(PlatformContext context, string clientID)
        {                
            try
            {
                return context.Client.Find(clientID);
            }
            catch (Exception)
            {
                return null;
            }            
        }
    }
}
