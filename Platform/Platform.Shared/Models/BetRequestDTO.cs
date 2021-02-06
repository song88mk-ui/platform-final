using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetRequestDTO
    {
        public BetRequestDTO()
        {
            Selections = new List<BetSelectionRequestDTO>();
        }
        public string APIKey { get; set; }
        public string OperatorToken { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public decimal Stake { get; set; }
        public DateTime TransactionDate { get; set; }
        public OddsType OddsType { get; set; }
        public CurrencyCode CurrencyCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public Channel Channel { get; set; }
        public string IPAddress { get; set; }
        public List<BetSelectionRequestDTO> Selections { get; set; }
    }

    public class BetSelectionRequestDTO
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public int MarketTemplateId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public decimal Line { get; set; }
        public int SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
        public string SelectionName { get; set; }
        public decimal Odds { get; set; }
        public FixtureStatus FixtureStatus { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string BetHomeScore { get; set; }
        public string BetAwayScore { get; set; }
        public string FixtureSpecifier { get; set; }
        public string SelectionSpecifier { get; set; }
        public string CorrelationId { get; set; }
    }
}
