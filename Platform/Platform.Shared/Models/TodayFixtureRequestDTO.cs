using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class TodayFixtureRequestDTO
    {
        public int MinuteUTCOffset { get; set; }
        public int SportId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public string ClientCode { get; set; }
        public string IpCountryCode { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
    }
}
