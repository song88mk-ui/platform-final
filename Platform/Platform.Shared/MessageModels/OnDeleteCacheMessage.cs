using Platform.Shared.Enums;
using System;

namespace Platform.Shared.MessageModels
{
    public class OnDeleteCacheMessage
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public CacheType CacheType { get; set; }
        public string CacheKey { get; set; }
    }
}
