using Platform.Shared.Enums;

namespace Platform.Shared.Models
{
    public class CachePreloadRequestDTO
    {
        public CacheType CacheType { get; set; }
        public string CacheKey { get; set; }
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
