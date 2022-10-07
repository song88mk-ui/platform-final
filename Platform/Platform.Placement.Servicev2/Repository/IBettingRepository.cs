using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;

namespace Platform.Placement.Servicev2.Repository
{
    public interface IBettingRepository
    {
        void ProcesssBetPlacement(OnBetPlacementDTO message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId);
    }
}
