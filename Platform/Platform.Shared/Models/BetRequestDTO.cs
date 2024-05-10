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
        public bool IsAgent { get; set; }
        public string OperatorToken { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int ClientIdRef { get; set; }
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
        public string IPAddressV4 { get; set; }
        public string IPAddressV6 { get; set; }
        public bool IsV4 { get; set; }
        public bool IsBoosted { get; set; }
        public bool IsBetBuilder { get; set; }
        public string BonusId { get; set; }
        public string UserAgent { get; set; } = string.Empty;
        public bool IsOGClient { get; set; }
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
        public string MarketTemplateId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public string SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
        public string SelectionName { get; set; }
        public decimal DecimalOdds { get; set; }
        public decimal MalaysianOdds { get; set; }
        public decimal HongkongOdds { get; set; }
        public decimal IndonesianOdds { get; set; }
        public decimal OddsRef { get; set; }
        public FixtureStatus FixtureStatus { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string BetScore { get; set; }
        public string FixtureSpecifier { get; set; }
        public string SelectionSpecifier { get; set; }
        public string CorrelationId { get; set; }
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public bool IsBinary { get; set; }
        public int MarketSelectionCount { get; set; } = 0;
    }
}
