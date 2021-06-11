using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.CashOutStake.Service.Repository
{
    public interface IBettingRepository
    {
        void PreloadActiveBets();
        void AddBet(OnBetPlacement message);
        void RemoveBet(OnBetSettlement message);
        void ProcessCashOutStake(OnInternalOddsChangeMessage message, IMessageBroker messageBroker);
    }
}
