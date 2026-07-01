using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Sportsbook.API.Test.Repository
{
    // Minimal in-memory IMemCache double — no expiry semantics, just enough to observe
    // populate-then-hit behavior without a real distributed cache.
    internal sealed class FakeMemCache : IMemCache
    {
        private readonly Dictionary<string, object> _store = new Dictionary<string, object>();

        public void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout) => _store[key] = cacheObject;

        public void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date) => _store[key] = cacheObject;

        public T GetCache<T>(string key, Func<T> repositoryCall, int expiration) where T : class
        {
            if (_store.TryGetValue(key, out var value) && value is T typed)
                return typed;

            var result = repositoryCall();
            _store[key] = result;
            return result;
        }

        public T GetCache<T>(string key) where T : class
            => _store.TryGetValue(key, out var value) ? value as T : null;

        public KeyValuePair<bool, long> GetCache(string key) => default;

        public void DeleteCache(string key) => _store.Remove(key);

        public void DeleteCacheByPrefix(string prefix)
        {
            foreach (var key in _store.Keys.Where(k => k.StartsWith(prefix)).ToList())
                _store.Remove(key);
        }
    }
}
