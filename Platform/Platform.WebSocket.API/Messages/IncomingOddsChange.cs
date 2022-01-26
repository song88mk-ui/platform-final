using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.WebSocket.API.Messages
{
    public class IncomingOddsChange
    {
        public IncomingOddsChange()
        {
            Markets = new List<IncomingMarket>();
            SportTranslations = new List<InComingSportTranslation>();
            SegmentTranslations = new List<IncomingSegmentTranslation>();
            LeagueTranslations = new List<IncomingLeagueTranslation>();
            FixtureTranslations = new List<IncomingFixtureTranslation>();
        }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string FixtureName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public string Score { get; set; }
        public string FixtureSpecifier { get; set; }
        public bool IsOfferedLive { get; set; }
        public int HomeRedCards { get; set; }
        public int AwayRedCards { get; set; }
        public int CurrentPeriodId { get; set; }
        public string CurrentPeriod { get; set; }
        public int CurrentMinute { get; set; }
        public int FixtureTypeId { get; set; }
        public string FixtureTypeName { get; set; }
        public int TotalTime { get; set; }
        public string Favorite { get; set; }
        public int SortOrder { get; set; }
        public string ClientCodes { get; set; }
        public string CorrelationId { get; set; }
        public bool IsSuspended { get; set; }
        public bool? IsBoosted { get; set; }
        public List<InComingSportTranslation> SportTranslations { get; set; }
        public List<IncomingSegmentTranslation> SegmentTranslations { get; set; }
        public List<IncomingLeagueTranslation> LeagueTranslations { get; set; }
        public List<IncomingFixtureTranslation> FixtureTranslations { get; set; }
        public List<IncomingMarket> Markets { get; set; }
        public bool IsInsert { get; set; }
    }

    public class IncomingMarket
    {
        public IncomingMarket()
        {
            Selections = new List<IncomingSelection>();
            MarketTranslations = new List<IncomingMarketTranslation>();
        }
        public string ProviderFixtureId { get; set; }
        public long MarketId { get; set; }
        public string MarketTemplateId { get; set; }
        public string ProviderMarketId { get; set; }
        public string Line { get; set; }
        public MarketStatus MarketStatus { get; set; }
        public string MarketSpecifier { get; set; }
        public string MarketName { get; set; }
        public int SortOrder { get; set; }
        public bool IsInsert { get; set; }
        public string ClientCodes { get; set; }
        public string ClientCodeMargin { get; set; }
        public List<IncomingMarketTranslation> MarketTranslations { get; set; }
        public List<IncomingSelection> Selections { get; set; }
    }

    public class IncomingSelection
    {
        public IncomingSelection()
        {
            SelectionTranslations = new List<IncomingSelectionTranslation>();
        }
        public string ProviderFixtureId { get; set; }
        public string MarketTemplateId { get; set; }
        public string SelectionTemplateId { get; set; }
        public string MarketSpecifier { get; set; }
        public long SelectionId { get; set; }
        public decimal Odds { get; set; }
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public bool IsActive { get; set; }
        public string SelectionSpecifier { get; set; }
        public string SelectionName { get; set; }
        public int SortOrder { get; set; }
        public bool IsInsert { get; set; }
        public List<IncomingClientOdds> ClientOdds { get; set; }
        public List<IncomingSelectionTranslation> SelectionTranslations { get; set; }
    }

    public class InComingSportTranslation
    {
        public string LanguageCode { get; set; }
        public string SportName { get; set; }
    }
    public class IncomingSegmentTranslation
    {
        public string LanguageCode { get; set; }
        public string SegmentName { get; set; }
    }
    public class IncomingLeagueTranslation
    {
        public string LanguageCode { get; set; }
        public string LeagueName { get; set; }
    }
    public class IncomingFixtureTranslation
    {
        public string LanguageCode { get; set; }
        public string FixtureName { get; set; }
    }
    public class IncomingMarketTranslation
    {
        public string LanguageCode { get; set; }
        public string MarketName { get; set; }
    }
    public class IncomingSelectionTranslation
    {
        public string LanguageCode { get; set; }
        public string SelectionName { get; set; }
    }
    public class IncomingClientOdds
    {
        public string ClientCode { get; set; }
        public decimal Odds { get; set; }
        public string SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
    }
}
