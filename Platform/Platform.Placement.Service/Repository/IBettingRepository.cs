using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.Placement.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId);
    }
}
