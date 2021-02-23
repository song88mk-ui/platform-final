using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;

namespace Platform.Cancellation.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessMTSTimeout(OnBetTimeout message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId);
        void ProcessBackofficeCancellation(OnBetBackofficeCancellation message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId);
    }
}
