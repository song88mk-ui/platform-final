using Platform.Shared.Data.Betting;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;

namespace Platform.CashOutStake.Servicev2.Repository
{
    public interface IBettingRepository
    {
        void PreloadActiveBets();
        void AddBet(OnBetPlacement message);
        void RemoveBet(OnBetSettlement message);
        void ProcessOddsChange(OnExternalOddsChangeMessage message, IMessageBroker messageBroker);
        void ProcessOnBetSettlement(OnExternalBetSettlementMessage onBetSettlement, IMessageBroker messageBroker);
        void ProcessPartialCashOut(OnCashoutBetSettlement message, IMessageBroker messageBroker);
    }
}
