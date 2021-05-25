using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.Operator.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId);
        void ProcessBetFullCashOut(OnBetSettlement message, IMessageBroker messageBroker, string correlationId);
        void ProcesssBetSettlement(OnBetSettlement message, IMessageBroker messageBroker, string correlationId);
        void ProcesssBetCancellation(OnBetSettlement message, IMessageBroker messageBroker, string correlationId);
    }
}
