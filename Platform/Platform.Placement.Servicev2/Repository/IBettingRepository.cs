using Platform.Shared.MessageModels;
using Platform.Shared.Providers;
using Platform.Shared.Utility;

namespace Platform.Placement.Servicev2.Repository
{
    public interface IBettingRepository
    {
        void ProcesssBetPlacement(OnBetPlacement message, IBetProvider provider, IMessageBroker messageBroker, string correlationId, int reprocessAttempt = 0);
    }
}
