using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class BackofficeClientAllLeaguesRequestDTO
    {
        public int ClientIdRef { get; set; }
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
        public int LeagueId { get; set; }
        public int SegmentId { get; set; }
    }
}
