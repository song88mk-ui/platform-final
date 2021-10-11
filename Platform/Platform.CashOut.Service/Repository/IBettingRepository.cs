using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;

namespace Platform.CashOut.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessCashOut(OnCashoutBetSettlement message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId);
    }
}
