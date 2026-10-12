using Platform.Shared.MessageModels;
using System.Threading.Tasks;

namespace Platform.CacheSync.Service.Repository
{
    public interface ICacheSyncRepository
    {
        Task ProcessMarketSortOrderChange(OnDeleteCacheMessage message);
        Task ProcessSportSortOrderChange(OnDeleteCacheMessage message);
        Task ProcessOddstableLeagueSortOrderChange(OnDeleteCacheMessage message);
        Task ProcessUpdateUserFavorites(OnDeleteCacheMessage message);
        Task ProcessMaintenance(OnDeleteCacheMessage message);
        Task ProcessParlayBonusChange(OnDeleteCacheMessage message);
        Task ProcessClientChange(OnDeleteCacheMessage message);
        Task ProcessDynamicTranslationChange(OnDeleteCacheMessage message);
        Task ProcessTranslationChange(OnTranslationChangeMessage message);
        Task ProcessStaticTranslationChange(OnDeleteCacheMessage message);
    }
}
