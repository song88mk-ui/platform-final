using Platform.CacheSync.Service.Models;

namespace Platform.CacheSync.Service.Repository
{
    public interface ICacheSyncRepository
    {
        void ProcessMarketSortOrderChange(OnDeleteCacheMessage message);
        void ProcessSportSortOrderChange(OnDeleteCacheMessage message);
    }
}
