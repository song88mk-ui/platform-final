using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetRecommendationsResponseDTO
    {
        public BetRecommendationsResponseDTO()
        {
            Fixtures = new List<BetRecommendationFixtureResponseDTO>();
        }
        public List<BetRecommendationFixtureResponseDTO> Fixtures { get; set; }
    }
    public class BetRecommendationFixtureResponseDTO
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
        public bool IsHomeFavorite { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public bool IsLive { get; set; }
        public bool IsLiveStream { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public int FixtureTypeId { get; set; }
        public string Score { get; set; }
        public string CurrentPeriod { get; set; }
        public string CurrentMinute { get; set; }
        public List<int> BetStop { get; set; }
        public bool IsBetStop { get; set; }
        public bool IsBetStopGlobal { get; set; }
        public bool IsBetBuilder { get; set; }
        public int SortOrder { get; set; }
        public MarketDTO Market { get; set; }
    }
}
