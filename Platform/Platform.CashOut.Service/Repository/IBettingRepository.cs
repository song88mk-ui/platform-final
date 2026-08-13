using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.CashOut.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessCashOut(OnCashoutBetSettlement message, IMessageBroker messageBroker, string correlationId);
    }
}
