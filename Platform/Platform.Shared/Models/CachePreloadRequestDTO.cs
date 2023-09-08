using Platform.Shared.Enums;

namespace Platform.Shared.Models
{
    public class CachePreloadRequestDTO
    {
        public CacheType CacheType { get; set; }
        public string CacheKey { get; set; }
    }
}
