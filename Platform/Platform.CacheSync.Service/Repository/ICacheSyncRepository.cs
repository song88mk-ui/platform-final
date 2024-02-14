using Platform.Shared.MessageModels;

namespace Platform.CacheSync.Service.Repository
{
    public interface ICacheSyncRepository
    {
        void ProcessMarketSortOrderChange(OnDeleteCacheMessage message);
        void ProcessSportSortOrderChange(OnDeleteCacheMessage message);
        void ProcessUpdateUserFavorites(OnDeleteCacheMessage message);
        void ProcessMaintenance(OnDeleteCacheMessage message);
        void ProcessParlayBonusChange(OnDeleteCacheMessage message);
        void ProcessClientChange(OnDeleteCacheMessage message);
    }
}
