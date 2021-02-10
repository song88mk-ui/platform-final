using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.Settlement.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcesssBetSettlement(OnBetSettlement message, IMessageBroker messageBroker, string correlationId);
    }
}
