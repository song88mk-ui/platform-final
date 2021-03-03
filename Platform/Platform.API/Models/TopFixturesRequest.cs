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
}