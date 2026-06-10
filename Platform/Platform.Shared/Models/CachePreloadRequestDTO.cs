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
