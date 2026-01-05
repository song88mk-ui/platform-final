using Platform.Shared.MessageModels;
using Platform.Shared.Providers;

namespace Platform.Provider.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessTimeout(OnBetTimeout message, IBetProvider provider, string correlationId);
        void ProcessBackofficeCancellation(OnBetBackofficeCancellation message, IBetProvider provider, string correlationId);
    }
}
