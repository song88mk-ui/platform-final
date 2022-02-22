using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ManualBetDetailsResponseDTO
    {
        public ManualBetDetailsResponseDTO()
        {
            Bets = new List<ManualBetDetailsDTO>();
        }
        public List<ManualBetDetailsDTO> Bets { get; set; }
    }

    public class ManualBetDetailsDTO
    {
        public ManualBetDetailsDTO()
        {
            Selections = new List<ManualBetSelectionDetailsDTO>();
        }
        public Guid BetId { get; set; }
        public string UserId { get; set; }
        public decimal Stake { get; set; }
        public decimal TrueStake { get; set; }
        public decimal Odds { get; set; }
        public int OddsTypeId { get; set; }
        public string OddsTypeName { get; set; }
        public decimal Payout { get; set; }
        public decimal WinLoss { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Reason { get; set; }
        public string CurrencyCode { get; set; }
        public string LanguageCode { get; set; }
        public decimal ExchangeRate { get; set; }
        public string IPAddress { get; set; }
        public string Channel { get; set; }
        public string ClientId { get; set; }
        public int ClientIdRef { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string LimitId { get; set; }
        public string Endpoint { get; set; }
        public string OperatorToken { get; set; }
        public decimal CompanyShare { get; set; }
        public bool IsResettled { get; set; }
        public bool IsCashout { get; set; }
        public DateTime BetDate { get; set; }
        public DateTime BetDateOffset { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime SettlementDateOffset { get; set; }
        public decimal StakeTax { get; set; }
        public decimal WinningsTax { get; set; }
        public decimal TaxedStake { get; set; }
        public decimal TaxedWinnings { get; set; }
        public decimal ParlayBonus { get; set; }
        public decimal ParlayBonusWinnings { get; set; }
        public decimal StakeReal { get; set; }
        public decimal StakeBonus { get; set; }
        public decimal StakeRealPayout { get; set; }
        public decimal StakeBonusPayout { get; set; }
        public bool IsFreeBet { get; set; }
        public bool IsBoosted { get; set; }
        public List<ManualBetSelectionDetailsDTO> Selections { get; set; }
    }

    public class ManualBetSelectionDetailsDTO
    {
        public Guid BetId { get; set; }
        public Guid BetSelectionId { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureId { get; set; }
        public string FixtureName { get; set; }
        public int FixtureTypeId { get; set; }
        public string FixtureTypeName { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        public string SelectionTemplateId { get; set; }
        public string SelectionId { get; set; }
        public string SelectionName { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public string Line { get; set; }
        public decimal Odds { get; set; }
        public int BetSelectionStatusId { get; set; }
        public string BetSelectionStatusName { get; set; }
        public bool IsSettled { get; set; }
        public bool IsResettled { get; set; }
        public string FixtureSpecifier { get; set; }
        public string SelectionSpecifier { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string BetScore { get; set; }
        public string SettlementScore { get; set; }
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public string CorrelationId { get; set; }
        public decimal DeadHeatFactor { get; set; }
    }
}
