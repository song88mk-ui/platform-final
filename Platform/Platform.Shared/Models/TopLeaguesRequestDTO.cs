using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class TopLeaguesRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public int SportId { get; set; }
        public string ClientCode { get; set; }
        public FixtureStatusGroup FixtureStatusGroup { get; set; }
        public int MinuteUTCOffset { get; set; }
    }

    public class TopLeaguesRequest2DTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public int SportId { get; set; }
        public int LeagueId { get; set; }
        public int SegmentId { get; set; }
        public string ClientCode { get; set; }        
    }
}
