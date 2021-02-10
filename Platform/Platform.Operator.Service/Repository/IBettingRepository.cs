using Platform.Operator.Service.Utility;
using Platform.Shared.MessageModels;

namespace Platform.Operator.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId);
        void ProcesssBetSettlement(OnBetSettlement message, IMessageBroker messageBroker, string correlationId);
    }
}
