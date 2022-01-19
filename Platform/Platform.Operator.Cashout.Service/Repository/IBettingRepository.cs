using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.Operator.Cashout.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessCashOutResult(OnCashOutResult message, IMessageBroker messageBroker, string correlationId);
    }
}
