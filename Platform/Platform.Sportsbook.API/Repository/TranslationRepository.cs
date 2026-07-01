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

        // Content-keyed lookup against TranslationCache (non-English translations, written by the
        // external translation script). Input maps each entity key ("MaId:123") -> its source text
        // ("Money Line"). Translations are matched by SOURCE_HASH (= hash of the source text), so
        // identical texts share one translation. Result stays keyed by the original entity key, so
        // callers (GetTranslation) are unchanged. English is the base language -- no lookup; callers
        // fall back to the stored source name.
        private const string ENGLISH_LANGUAGE = "EN";

        public async Task<Dictionary<string, string>> GetTranslationsByContentAsync(
            Dictionary<string, string> keyToSourceText,
            string languageCode)
        {
            var result = new Dictionary<string, string>();

            if (keyToSourceText == null || keyToSourceText.Count == 0 ||
                string.Equals(languageCode, ENGLISH_LANGUAGE, StringComparison.OrdinalIgnoreCase))
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

                // TranslationCache PK is (SOURCE_HASH, SOURCE_LANGUAGE, TARGET_LANGUAGE), so at most
                // one row can match per hash -- no version tie-break needed.
                var data = await _context.TranslationCache
                    .AsNoTracking()
                    .Where(x =>
                        missingList.Contains(x.SourceHash)
                        && x.SourceLanguage == ENGLISH_LANGUAGE
                        && x.TargetLanguage == languageCode
                        && !string.IsNullOrEmpty(x.TargetText))
                    .Select(x => new { x.SourceHash, x.TargetText })
                    .ToListAsync();

                foreach (var item in data)
                {
                    hashToTarget[item.SourceHash] = item.TargetText;

                    var cacheKey = $"{CACHE_PREFIX}h:{languageCode}:{item.SourceHash}";
                    _memCache.AddOrUpdateCache(cacheKey, item.TargetText, DateTimeOffset.UtcNow.AddHours(6));
                }
            }

            foreach (var kvp in keyToHash)
            {
                if (hashToTarget.TryGetValue(kvp.Value, out var target) && !string.IsNullOrEmpty(target))
                    result[kvp.Key] = target;
            }

            return result;
        }

        // Resolves NON-English translations by looking up TranslationCache.STRING_ID (written by the
        // external translation script) for the requested TARGET_LANGUAGE. Input maps a caller key (e.g.
        // "MaId:123") -> the SportsbookGlossary-style STRING_ID for that entity ("MaTe:1234", "Te:789",
        // ...); output maps the same key -> translated text. English is the base — no lookup; callers
        // fall back to the stored name. Identical STRING_IDs dedupe naturally (shared cache entry).
        // Cache namespace sb:translation:s:{lang}:{stringId}; InvalidateTranslationsCache(purgeAll)
        // clears it.
        public async Task<Dictionary<string, string>> GetTranslationCacheAsync(
            Dictionary<string, string> keyToStringId,
            string languageCode)
        {
            // English is the base language — no translation lookup; callers use the stored name.
            if (keyToStringId == null || keyToStringId.Count == 0 ||
                string.Equals(languageCode, ENGLISH_LANGUAGE, StringComparison.OrdinalIgnoreCase))
                return new Dictionary<string, string>();

            // STRING_ID -> target text (cache-first, keyed by STRING_ID so identical keys share one entry)
            var stringIdToTarget = new Dictionary<string, string>();
            var missingStringIds = new HashSet<string>();

            foreach (var stringId in keyToStringId.Values.Distinct())
            {
                if (string.IsNullOrWhiteSpace(stringId))
                    continue;

                var cacheKey = $"{CACHE_PREFIX}s:{languageCode}:{stringId}";
                var cacheValue = _memCache.GetCache<string>(cacheKey);

                if (!string.IsNullOrWhiteSpace(cacheValue))
                    stringIdToTarget[stringId] = cacheValue;
                else
                    missingStringIds.Add(stringId);
            }

            if (missingStringIds.Count > 0)
            {
                var missingList = missingStringIds.ToList();

                var data = await _context.TranslationCache
                    .AsNoTracking()
                    .Where(c =>
                        missingList.Contains(c.StringId)
                        && c.TargetLanguage == languageCode
                        && c.TargetText != null && c.TargetText != "")
                    .Select(c => new { c.StringId, c.TargetText })
                    .ToListAsync();

                foreach (var item in data)
                {
                    stringIdToTarget[item.StringId] = item.TargetText;

                    var cacheKey = $"{CACHE_PREFIX}s:{languageCode}:{item.StringId}";
                    _memCache.AddOrUpdateCache(cacheKey, item.TargetText, DateTimeOffset.UtcNow.AddHours(6));
                }
            }

            var result = new Dictionary<string, string>();
            foreach (var kvp in keyToStringId)
                if (stringIdToTarget.TryGetValue(kvp.Value, out var target) && !string.IsNullOrEmpty(target))
                    result[kvp.Key] = target;

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
