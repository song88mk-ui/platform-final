using Platform.Shared.MessageModels;
using Platform.Shared.Providers;
using System;
using System.Threading.Tasks;

namespace Platform.Provider.Service.Repository
{
    public interface IBettingRepository
    {
        void ProcessBetCancellation(Guid betId, IBetProvider provider, string correlationId, bool isBackOfficeTrigger = false);
        void ProcessBetSettlement(OnBetSettlement message, IBetProvider provider, string correlationId);
    }
}
