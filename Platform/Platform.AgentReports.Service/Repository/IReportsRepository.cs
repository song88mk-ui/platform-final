using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.AgentReports.Service.Repository
{
    public interface IReportsRepository
    {
        void ProcessBetPlacement(OnBetPlacement message);
        void ProcessBetFullCashOut(OnBetSettlement message, IMessageBroker messageBroker);
        void ProcessBetSettlement(OnBetSettlement onBetSettlement, IMessageBroker messageBroker);
        void ProcessBetCancellation(OnBetSettlement onBetCancellation, IMessageBroker messageBroker);
        void ProcessCasinoBetSettlement(OnCasinoBetSettlement message, IMessageBroker messageBroker);
    }
}
