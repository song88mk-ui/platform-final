using Platform.Shared.Enums;
using System;

namespace Platform.CacheSync.Service.Models
{
    public class OnDeleteCacheMessage
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public CacheType CacheType { get; set; }
    }
}
