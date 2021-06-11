namespace Platform.CashOutStake.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessCashOutStake(OnInternalOddsChangeMessage message);
    }
}
