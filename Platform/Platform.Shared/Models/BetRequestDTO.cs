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
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public string Endpoint { get; set; }
        public decimal CompanyShare { get; set; }
        public decimal ExchangeRate { get; set; }
        public Guid BetId { get; set; }
        public string UserId { get; set; }
        public decimal Stake { get; set; }
        public decimal Odds { get; set; }
        public string BetStatus { get; set; }
        public DateTime BetDate { get; set; }
        public OddsType OddsType { get; set; }
        public CurrencyCode CurrencyCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public Channel Channel { get; set; }
        public string IPAddress { get; set; }
        public List<BetSelectionRequestDTO> Selections { get; set; }
    }

    public class BetSelectionRequestDTO
    {
        public Guid BetSelectionId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public FixtureType FixtureType { get; set; }
        public int MarketTemplateId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
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
