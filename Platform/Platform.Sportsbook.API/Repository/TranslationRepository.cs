using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Shared.Data.Sportsbook;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Repository
{
    public class TranslationRepository : ITranslationRepository
    {
        #region Fields

        private readonly TranslationContext _context;
        private readonly IMemCache _memCache;
        private readonly ILogger<TranslationRepository> _logger;

        private const int CACHE_DURATION_MINUTES = 60;
        private const string CACHE_PREFIX = "sb:translation:";

        #endregion

        #region Ctor

        public TranslationRepository(
            TranslationContext context,
            IMemCache memCache,
            ILogger<TranslationRepository> logger)
        {
            _context = context;
            _memCache = memCache;
            _logger = logger;
        }

        #endregion

        #region Methods

        public async Task<Dictionary<string, string>> GetTranslationsAsync(
            List<string> stringIds,
            string languageCode)
        {
            if (stringIds == null || stringIds.Count == 0)
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();

            var missingKeys = new List<string>();

            // =========================
            // CHECK MEMCACHE FIRST
            // =========================
            foreach (var stringId in stringIds.Distinct())
            {
                var cacheKey = $"{CACHE_PREFIX}{languageCode}:{stringId}";

                var cacheValue = _memCache.GetCache<string>(cacheKey);

                if (!string.IsNullOrWhiteSpace(cacheValue))
                {
                    result[stringId] = cacheValue;
                }
                else
                {
                    missingKeys.Add(stringId);
                }
            }

            // =========================
            // SQL FALLBACK
            // =========================
            if (missingKeys.Count > 0)
            {
                var data = await _context.SportsbookDynamic
                    .AsNoTracking()
                    .Where(x =>
                        missingKeys.Contains(x.StringId)
                        && x.TargetLanguageId == languageCode
                        && x.Processed
                        && !string.IsNullOrEmpty(x.TargetText))
                    .Select(x => new
                    {
                        x.StringId,
                        x.TargetText
                    })
                    .ToListAsync();

                foreach (var item in data)
                {
                    result[item.StringId] = item.TargetText;

                    var cacheKey = $"{CACHE_PREFIX}{languageCode}:{item.StringId}";

                    _memCache.AddOrUpdateCache(
                        cacheKey,
                        item.TargetText,
                        DateTimeOffset.UtcNow.AddHours(6));
                }
            }

            return result;
        }

        // Content-keyed lookup (proposal: docs/proposals/TranslationContentKeying.md).
        // Input maps each entity key ("MaId:123") -> its source text ("Money Line").
        // Translations are matched by SOURCE_HASH (= hash of the source text), so identical
        // texts share one translation. Result stays keyed by the original entity key, so
        // callers (GetTranslation) are unchanged.
        public async Task<Dictionary<string, string>> GetTranslationsByContentAsync(
            Dictionary<string, string> keyToSourceText,
            string languageCode)
        {
            var result = new Dictionary<string, string>();

            if (keyToSourceText == null || keyToSourceText.Count == 0)
                return result;

            // entity key -> hash of its source text
            var keyToHash = new Dictionary<string, string>();
            foreach (var kvp in keyToSourceText)
            {
                if (string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                keyToHash[kvp.Key] = TranslationHashing.SourceHash(kvp.Value);
            }

            if (keyToHash.Count == 0)
                return result;

            // hash -> target text (cache first, keyed by hash so identical texts share an entry)
            var hashToTarget = new Dictionary<string, string>();
            var missingHashes = new HashSet<string>();

            foreach (var hash in keyToHash.Values.Distinct())
            {
                var cacheKey = $"{CACHE_PREFIX}h:{languageCode}:{hash}";
                var cacheValue = _memCache.GetCache<string>(cacheKey);

                if (!string.IsNullOrWhiteSpace(cacheValue))
                    hashToTarget[hash] = cacheValue;
                else
                    missingHashes.Add(hash);
            }

            if (missingHashes.Count > 0)
            {
                var missingList = missingHashes.ToList();

                var data = await _context.SportsbookDynamic
                    .AsNoTracking()
                    .Where(x =>
                        missingList.Contains(x.SourceHash)
                        && x.TargetLanguageId == languageCode
                        && x.Processed
                        && !string.IsNullOrEmpty(x.TargetText))
                    .Select(x => new { x.SourceHash, x.TargetText, x.GlossaryVersion })
                    .ToListAsync();

                // Several rows can share a hash (pre-dedup, or multiple glossary versions);
                // take the highest glossary version.
                foreach (var grp in data.GroupBy(x => x.SourceHash))
                {
                    var chosen = grp.OrderByDescending(x => x.GlossaryVersion).First().TargetText;
                    hashToTarget[grp.Key] = chosen;

                    var cacheKey = $"{CACHE_PREFIX}h:{languageCode}:{grp.Key}";
                    _memCache.AddOrUpdateCache(cacheKey, chosen, DateTimeOffset.UtcNow.AddHours(6));
                }
            }

            foreach (var kvp in keyToHash)
            {
                if (hashToTarget.TryGetValue(kvp.Value, out var target) && !string.IsNullOrEmpty(target))
                    result[kvp.Key] = target;
            }

            return result;
        }

        public void InvalidateTranslationsCache(
            List<string> stringIds,
            string languageCode,
            bool purgeAll = false)
        {
            if (purgeAll == false && (stringIds == null || stringIds.Count == 0))
                return;

            if(purgeAll)
            {
                _memCache.DeleteCacheByPrefix(CACHE_PREFIX);
                return;
            }

            foreach (var stringId in stringIds.Distinct())
            {
                var cacheKey = $"{CACHE_PREFIX}{languageCode}:{stringId}";
                _memCache.DeleteCache(cacheKey);
            }
        }

        #endregion
    }
}
