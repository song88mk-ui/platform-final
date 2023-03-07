using Platform.Shared.MessageModels;

namespace Platform.AgentReports.Service.Repository
{
    public interface IReportsRepository
    {
        void ProcessBetPlacement(OnBetPlacement message);
        void ProcessBetFullCashOut(OnBetSettlement message);
        void ProcessBetSettlement(OnBetSettlement onBetSettlement);
        void ProcessBetCancellation(OnBetSettlement onBetCancellation);
    }
}
