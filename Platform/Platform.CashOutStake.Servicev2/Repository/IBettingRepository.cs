using Platform.Shared.Data.Betting;
using Platform.Shared.Utility;
using System;

namespace Platform.CashOutStake.Servicev2.Repository
{
    public interface IBettingRepository
    {
        void PreloadActiveBets();
        void AddBet(BetMemory bet, BettingContext context);
        void RemoveBet(Guid betId);
        void ProcessOddsChange(OnExternalOddsChangeMessage message, IMessageBroker messageBroker);
    }
}
