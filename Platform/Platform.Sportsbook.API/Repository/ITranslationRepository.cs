using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Repository
{
    public interface ITranslationRepository
    {
        Task<Dictionary<string, string>> GetTranslationsAsync(
            List<string> stringIds,
            string languageCode);

        // Resolves non-English translations by looking up TranslationCache.SOURCE_HASH (content-hash
        // of the source text). Default active path. Input maps a caller key -> its source text; output
        // maps the same key -> translated text (English yields an empty map — callers fall back to the
        // stored name).
        Task<Dictionary<string, string>> GetTranslationsByContentAsync(
            Dictionary<string, string> keyToSourceText,
            string languageCode);

        // Resolves non-English translations by looking up TranslationCache.STRING_ID — the same key
        // bet-genius writes to SportsbookGlossary (e.g. "MaTe:1234", "SeTe:1234:56", "Te:789"). Input
        // maps a caller key -> the glossary STRING_ID for that entity; output maps the same key ->
        // translated text (English yields an empty map — callers fall back to the stored name).
        Task<Dictionary<string, string>> GetTranslationCacheAsync(
            Dictionary<string, string> keyToStringId,
            string languageCode);

        void InvalidateTranslationsCache(
            List<string> stringIds,
            string languageCode,
            bool purgeAll = false);
    }
}
