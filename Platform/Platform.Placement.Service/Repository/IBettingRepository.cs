using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;

namespace Platform.Placement.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcesssBetPlacement(OnBetPlacement message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId);
    }
}
