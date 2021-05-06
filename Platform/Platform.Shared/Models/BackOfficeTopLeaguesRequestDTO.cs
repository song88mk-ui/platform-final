using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BackOfficeTopLeaguesRequestDTO
    {
        public string ClientIdRefs { get; set; }
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
    }
}
