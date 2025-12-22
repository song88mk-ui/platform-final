using Platform.Shared.MessageModels;
using Platform.Shared.Providers;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;

namespace Platform.Cancellation.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessTimeout(OnBetTimeout message, IBetProvider provider, string correlationId);
        void ProcessBackofficeCancellation(OnBetBackofficeCancellation message, IBetProvider provider, string correlationId);
    }
}
