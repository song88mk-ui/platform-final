using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace Platform.Shared.Utility
{
    public class MemCache : IMemCache
    {
        public void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout)
        {
            MemoryCache.Default.Set(key, cacheObject, new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddHours(cacheTimeout)
            });
        }

        public void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date)
        {
            MemoryCache.Default.Set(key, cacheObject, new CacheItemPolicy
            {
                AbsoluteExpiration = date
            });
        }

        public T GetCache<T>(string key, Func<T> repositoryCall, int expiration) where T : class
        {
            var result = (T)MemoryCache.Default.Get(key);

            if (result == null && repositoryCall != null)
            {
                result = repositoryCall();

                if (result != null)
                    AddOrUpdateCache(key, result, expiration);
            }

            return result;
        }

        public KeyValuePair<bool, long> GetCache(string key)
        {
            var result = MemoryCache.Default.Get(key);

            return result == null ? new KeyValuePair<bool, long>(false, 0) : (KeyValuePair<bool, long>)result;
        }

        public T GetCache<T>(string key) where T : class
        {
            var result = (T)MemoryCache.Default.Get(key);

            return result;
        }

        public void DeleteCache(string key)
        {
            MemoryCache.Default.Remove(key);
        }

        public void DeleteCacheByPrefix(string prefix)
        {
            var keys = MemoryCache.Default
                .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keys)
            {
                MemoryCache.Default.Remove(key);
            }
        }
    }
}
