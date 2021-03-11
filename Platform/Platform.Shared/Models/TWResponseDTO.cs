using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class TWFixtureDetailsRequestDTO
    {
        public string FixtureId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class TWTopLeagueFixtureRequestDTO
    {
        public int SportId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class TWSearchRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public string SearchText { get; set; }
    }
    public class TWSearchResponseDTO
    {
        public TWSearchResponseDTO()
        {
            SearchResult = new List<SearchResponse>();
        }
        public List<SearchResponse> SearchResult { get; set; }
    }
    public class SearchResponse
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureId { get; set; }
        public string FixtureName { get; set; }
    }
    public class TWFixtureRequestDTO
    {
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class TWSegmentFixtureRequestDTO
    {
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class TWLiveFixtureRequestDTO
    {
        public int SportId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class TWOutrightFixtureRequestDTO
    {
        public int SportId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class TWTodayFixtureRequestDTO
    {
        public int MinuteUTCOffset { get; set; }
        public int SportId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class TWDailyFixtureRequestDTO
    {
        public DateTime DateUTC { get; set; }
        public int MinuteUTCOffset { get; set; }
        public int SportId { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class TWFTodayFixtureCountRequestDTO
    {
        public int MinuteUTCOffset { get; set; }
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
    public class TWFLiveFixtureCountRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }

    public class TWFixtureCountResponseDTO
    {
        public TWFixtureCountResponseDTO()
        {
            Sports = new List<FixtureCount>();
        }
        public List<FixtureCount> Sports { get; set; }
    }

    public class FixtureCount
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int Count { get; set; }
    }

    public class TempFixtureCount : FixtureCount
    {
        public int SortOrder { get; set; }
    }

    public class TWRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }

    public class TWResponseDTO
    {
        public TWResponseDTO()
        {
            Fixtures = new List<FixtureDTO>();
        }
        public List<FixtureDTO> Fixtures { get; set; }
    }

    public class FixtureDTO
    {
        public FixtureDTO()
        {
            Markets = new List<MarketDTO>();
        }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string FixtureName { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public bool IsLive { get; set; }
        public bool IsLiveStream { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public int MarketCount { get; set; }
        public string SMSCode { get; set; }
        public string StatisticsURL { get; set; }
        public string Score { get; set; }
        public string CurrentPeriod { get; set; }
        public string CurrentMinute { get; set; }
        public SportMarketTemplateGroup MarketTemplateGroups { get; set; }
        public List<MarketDTO> Markets { get; set; }
    }

    public class MarketDTO
    {
        public MarketDTO()
        {
            Selections = new List<SelectionDTO>();
        }
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public bool IsCashout { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        public List<SelectionDTO> Selections { get; set; }
    }

    public class SelectionDTO
    {
        public string SelectionTemplateId { get; set; }
        public string SelectionId { get; set; }
        public string SelectionName { get; set; }
        public string DecimalOdds { get; set; }
        public bool IsActive { get; set; }
        public string CorrelationId { get; set; }
    }



    public class TempFixtureDTO
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string FixtureName { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public bool IsLive { get; set; }
        public bool IsLiveStream { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public int MarketCount { get; set; }
        public string SMSCode { get; set; }
        public string StatisticsURL { get; set; }
        public string Score { get; set; }
        public string CurrentPeriod { get; set; }
        public string CurrentMinute { get; set; }
        public int SortOrder { get; set; }
    }
}
