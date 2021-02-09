using Platform.Placement.Service.Utility;
using Platform.Shared.MessageModels;

namespace Platform.Placement.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        public void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId)
        {

        }
    }
}
