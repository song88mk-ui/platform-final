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

        public void InvalidateTranslationsCache(
            List<string> stringIds,
            string languageCode)
        {
            if (stringIds == null || stringIds.Count == 0)
                return;

            foreach (var stringId in stringIds.Distinct())
            {
                var cacheKey = $"{CACHE_PREFIX}{languageCode}:{stringId}";
                _memCache.DeleteCache(cacheKey);
            }
        }

        #endregion
    }
}
