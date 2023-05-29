using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.Settlement.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessBetSettlement(OnUOFBetSettlement message, IMessageBroker messageBroker, string correlationId);
        void ProcessBetCancellation(OnUOFBetCancellation message, IMessageBroker messageBroker, string correlationId);
        void ProcessBackOfficeBetCancellation(OnBOBetCancellaton message, IMessageBroker messageBroker, string correlationId);
        void ProcessBackOfficeManualSettlement(OnBOBetManualSettlement message, IMessageBroker messageBroker, string correlationId);
    }
}
