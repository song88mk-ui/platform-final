using Platform.Shared.Data.Betting;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;
using System.Threading.Tasks;

namespace Platform.SettlementScore.Service.Repository
{
    public interface IBettingRepository
    {
        void PreloadActiveBets();
        void AddBet(OnBetPlacement message);
        Task ProcessBet(OnBetSettlement message);
    }
}
