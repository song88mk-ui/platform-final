using Platform.Shared.MessageModels;

namespace Platform.CacheSync.Service.Repository
{
    public interface ICacheSyncRepository
    {
        void ProcessMarketSortOrderChange(OnDeleteCacheMessage message);
        void ProcessSportSortOrderChange(OnDeleteCacheMessage message);
        void ProcessUpdateUserFavorites(OnDeleteCacheMessage message);
    }
}
