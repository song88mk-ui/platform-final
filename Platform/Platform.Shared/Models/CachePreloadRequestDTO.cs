using Platform.Shared.Enums;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class CachePreloadRequestDTO
    {
        public CacheType CacheType { get; set; }
        public string CacheKey { get; set; }
        public List<string> CacheKeys { get; set; } = new List<string>();
        public LanguageCode LanguageCode { get; set; } = LanguageCode.EN;
        public bool PurgeAll { get; set; } = false;

        // Changed TranslationCache2 rows (external translation script -> POST v1/cachepreload with
        // CacheType.DynamicTranslation). When present, Platform.API publishes an
        // OnTranslationChange message to Platform.CacheSync.Service so the change propagates to the
        // OE.Adapter translation cache, MongoDB, and websocket clients.
        public List<TranslationChangeRequestDTO> Translations { get; set; }
    }

    public class TranslationChangeRequestDTO
    {
        public string StringId { get; set; }
        public string TargetLanguage { get; set; }
        public string SourceText { get; set; }
        public string TargetText { get; set; }
    }

    public class LeagueTierRatingResponseDTO
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int Tier { get; set; }
    }
}
