using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnDeleteCacheMessage
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public CacheType CacheType { get; set; }
        public string CacheKey { get; set; } = string.Empty;
        public List<string> CacheKeys { get; set; } = new List<string>();
        public LanguageCode LanguageCode { get; set; } = LanguageCode.EN;
    }
}
