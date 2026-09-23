using Platform.Shared.Data.Sportsbook.Entities;
using System;
using System.Collections.Generic;

namespace Platform.Sportsbook.API.Utility
{
    // Name resolution for the Mongo-sourced translations (2026-07 translation-flow redesign; the
    // only path since the UseMongoTranslations flag and the legacy SQL cachekey resolution were
    // removed). Names come from the per-language *NaTr lists already fetched with the documents
    // (ElemMatch projections), so nothing here does I/O.
    //
    // The contract throughout: an untranslated entity renders its original English name, never
    // blank. FindTranslation returns null for "no translation", AddTranslation then leaves the key
    // out of the map, and ResolveName falls back to the English name the caller passes in.
    public static class MongoTranslationResolver
    {
        // Returns null, not an empty string, when the language is absent or its text is empty,
        // so every caller treats "no translation" the same way and falls back to English.
        public static string FindTranslation<T>(
            List<T> translations,
            string languageCode,
            Func<T, string> languageSelector,
            Func<T, string> textSelector)
        {
            if (translations == null)
                return null;

            foreach (var translation in translations)
            {
                if (translation != null && string.Equals(languageSelector(translation), languageCode, StringComparison.OrdinalIgnoreCase))
                {
                    var text = textSelector(translation);

                    return string.IsNullOrEmpty(text) ? null : text;
                }
            }

            return null;
        }

        // A key is added only when there is something to add: a missing key is what makes
        // ResolveName hand back the English name.
        public static void AddTranslation(Dictionary<string, string> map, string key, string translated)
        {
            if (!string.IsNullOrEmpty(translated))
                map[key] = translated;
        }

        // Fixture names for non-EN languages are composed from the two translated team names
        // (GetHomeName/GetAwayName downstream split on " vs. ").
        public static string ComposeFixtureName(
            List<TeamTranslationMDB> hoNaTr,
            List<TeamTranslationMDB> awNaTr,
            string languageCode)
        {
            return ComposeFixtureName(hoNaTr, awNaTr, languageCode, null, null);
        }

        // Overload used where the English team names are available as a per-side fallback: a team
        // with no translated entry still renders in English rather than reverting BOTH sides to
        // English just because its opponent's translation is missing (matches the WebSocket push
        // path in TranslationDeltaApplier, which updates Home/Away independently).
        public static string ComposeFixtureName(
            List<TeamTranslationMDB> hoNaTr,
            List<TeamTranslationMDB> awNaTr,
            string languageCode,
            string hoNaFallback,
            string awNaFallback)
        {
            var home = FindTranslation(hoNaTr, languageCode, t => t.LaCo, t => t.TeNa) ?? hoNaFallback;
            var away = FindTranslation(awNaTr, languageCode, t => t.LaCo, t => t.TeNa) ?? awNaFallback;

            return !string.IsNullOrEmpty(home) && !string.IsNullOrEmpty(away)
                ? $"{home} vs. {away}"
                : null;
        }

        // The English fallback itself: no id, or no entry under "{prefix}:{id}", means the caller's
        // original English name is returned unchanged.
        public static string ResolveName(
            Dictionary<string, string> translations,
            object id,
            string prefix,
            string fallback)
        {
            if (id == null)
                return fallback;

            var key = $"{prefix}:{id}";

            if (translations.TryGetValue(key, out var translated))
                return translated;

            return fallback;
        }
    }
}
