using Platform.Shared.Enums;
using System;

namespace Platform.API.Models
{
    public class TopFixturesRequest
    {
        public string LanguageCode { get; set; }
    }

    public class FixtureCountRequest
    {
        public string LanguageCode { get; set; }
    }
    public class TodayFixtureCountRequest
    {
        public int HourUTCOffset { get; set; }
        public string LanguageCode { get; set; }
    }
    public class FixtureRequest
    {
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
    }

    public class TodayFixtureRequest
    {
        public int HourUTCOffset { get; set; }
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
    }

    public class LeagueFixtureRequest
    {
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public string LanguageCode { get; set; }
    }

    public class SportRequest
    {
        public string LanguageCode { get; set; }
    }

    public class FixtureDetailsRequest
    {
        public string FixtureId { get; set; }
        public string LanguageCode { get; set; }
    }
}