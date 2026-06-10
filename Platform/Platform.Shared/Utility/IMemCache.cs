using System;
using System.Collections.Generic;

namespace Platform.Shared.Utility
{
    public interface IMemCache
    {
        void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout);

        void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date);

        T GetCache<T>(string key, Func<T> repositoryCall, int expiration) where T : class;

        T GetCache<T>(string key) where T : class;

        KeyValuePair<bool, long> GetCache(string key);
        void DeleteCache(string key);

        void DeleteCacheByPrefix(string prefix);
    }
}
