using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OperatorBetSettlementRequestDTO
    {
        public OperatorBetSettlementRequestDTO()
        {
            BetSelections = new List<OperatorBetSelectionSettlementRequestDTO>();
        }

        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; }

        [JsonPropertyName("betId")]
        public string BetId { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("payout")]
        public decimal Payout { get; set; }

        [JsonPropertyName("stake")]
        public decimal Stake { get; set; }

        [JsonPropertyName("odds")]
        public decimal Odds { get; set; }

        [JsonPropertyName("betStatus")]
        public string BetStatus { get; set; }

        [JsonPropertyName("stakeTax")]
        public decimal StakeTax { get; set; }

        [JsonPropertyName("winningsTax")]
        public decimal WinningsTax { get; set; }

        [JsonPropertyName("taxedStake")]
        public decimal TaxedStake { get; set; }

        [JsonPropertyName("taxedWinnings")]
        public decimal TaxedWinnings { get; set; }

        [JsonPropertyName("transactionDate")]
        public string TransactionDate { get; set; }

        [JsonPropertyName("betSelections")]
        public List<OperatorBetSelectionSettlementRequestDTO> BetSelections { get; set; }
    }

    public class OperatorBetSelectionSettlementRequestDTO
    {
        [JsonPropertyName("betSelectionId")]
        public string BetSelectionId { get; set; }

        [JsonPropertyName("betSelectionStatus")]
        public string BetSelectionStatus { get; set; }

        [JsonPropertyName("sportId")]
        public int SportId { get; set; }

        [JsonPropertyName("sportName")]
        public string SportName { get; set; }

        [JsonPropertyName("segmentId")]
        public int SegmentId { get; set; }

        [JsonPropertyName("segmentName")]
        public string SegmentName { get; set; }

        [JsonPropertyName("leagueId")]
        public int LeagueId { get; set; }

        [JsonPropertyName("leagueName")]
        public string LeagueName { get; set; }

        [JsonPropertyName("fixtureId")]
        public string FixtureId { get; set; }

        [JsonPropertyName("fixtureName")]
        public string FixtureName { get; set; }

        [JsonPropertyName("fixtureStartDate")]
        public string FixtureStartDate { get; set; }

        [JsonPropertyName("fixtureStatus")]
        public string FixtureStatus { get; set; }

        [JsonPropertyName("score")]
        public string Score { get; set; }

        [JsonPropertyName("settlementScore")]
        public string SettlementScore { get; set; }

        [JsonPropertyName("marketId")]
        public string MarketId { get; set; }

        [JsonPropertyName("marketName")]
        public string MarketName { get; set; }

        [JsonPropertyName("line")]
        public string Line { get; set; }

        [JsonPropertyName("selectionId")]
        public string SelectionId { get; set; }

        [JsonPropertyName("selectionName")]
        public string SelectionName { get; set; }

        [JsonPropertyName("odds")]
        public decimal Odds { get; set; }

        [JsonPropertyName("homeName")]
        public string HomeName { get; set; }

        [JsonPropertyName("awayName")]
        public string AwayName { get; set; }
    }
}
