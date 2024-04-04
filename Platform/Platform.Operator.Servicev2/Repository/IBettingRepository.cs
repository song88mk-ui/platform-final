using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;

namespace Platform.Operator.Servicev2.Repository
{
    public interface IBettingRepository
    {
        void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId, string threadId);
        void ProcesssBetPlacementOG(OnBetPlacement message, IMessageBroker messageBroker, string correlationId, string threadId);
        void ProcessBetPartialCashOut(OnBetSettlementPartial message, IMessageBroker messageBroker, string correlationId, string threadId);
        void ProcessBetFullCashOut(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId);
        void ProcesssBetSettlement(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId);
        void ProcesssBetSettlementOG(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId, OGRequestDTO ogParam);
        void ProcesssBetCancellation(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId);
    }
}
