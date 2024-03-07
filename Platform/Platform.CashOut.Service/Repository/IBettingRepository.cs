using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;

namespace Platform.CashOut.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessCashOut(OnCashoutBetSettlement message, CashoutType cashoutType, IMtsSdk mts, IMessageBroker messageBroker, string correlationId);
    }
}
