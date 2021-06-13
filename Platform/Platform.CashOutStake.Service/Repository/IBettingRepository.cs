using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.CashOutStake.Service.Repository
{
    public interface IBettingRepository
    {
        void PreloadActiveBets();
        void AddBet(OnBetPlacement message, long fixtureId);
        void RemoveBet(OnBetSettlement message, long fixtureId);
        void ProcessCashOutStake(OnInternalOddsChangeMessage message, long fixtureId, IMessageBroker messageBroker);
    }
}
