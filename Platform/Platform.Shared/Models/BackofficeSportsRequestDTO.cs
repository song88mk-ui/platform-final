using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class BackofficeSportsRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public int SportId { get; set; }
        public int LeagueId { get; set; }
        public int FixtureId { get; set; }
    }
}
