using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    
    public class BetBuilderFixtureResponseDTO
    {
        public BetBuilderFixtureResponseDTO()
        {
            Fixtures = new List<BetBuilderFixtureDTO>();
        }
        public List<BetBuilderFixtureDTO> Fixtures { get; set; }
    }

    public class BetBuilderFixtureDTO
    {
        public BetBuilderFixtureDTO()
        {
            Markets = new List<BetBuilderMarketDTO>();
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
        public List<int> BetStop { get; set; }
        public bool IsBetStopGlobal { get; set; }
        public int SortOrder { get; set; }
        public bool IsBoosted { get; set; }
        public SportMarketTemplateGroup MarketTemplateGroups { get; set; }
        public List<BetBuilderMarketDTO> Markets { get; set; }
    }

    public class BetBuilderMarketDTO
    {
        public BetBuilderMarketDTO()
        {
            Selections = new List<BetBuilderSelectionDTO>();
        }
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public bool IsCashout { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        public List<BetBuilderSelectionDTO> Selections { get; set; }
    }

    public class BetBuilderSelectionDTO
    {
        public string SelectionTemplateId { get; set; }
        public string SelectionId { get; set; }
        public string SelectionName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string CorrelationId { get; set; }
    }

}
