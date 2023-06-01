using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.CasinoBetting.Entities
{
    public class CasinoBet
    {
        public Guid BetId { get; set; }
        public string ProviderTransactionId { get; set; }
        public string TransactionType { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Endpoint { get; set; }
        public string UserId { get; set; }
        public string OperatorToken { get; set; }
        public bool IsAgent { get; set; }
        public string RoundId { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal Stake { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal StakeBonus { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal WinLoss { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal Payout { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Reason { get; set; }
        public string CurrencyCode { get; set; }
        public string GameId { get; set; }
        public string Device { get; set; }
        public string ClientType { get; set; }
        public string ProviderBetId { get; set; }
        public string Category { get; set; }
        public bool IsComplete { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal ExchangeRate { get; set; }
        public string JackpotContributions { get; set; }
        public string ConversionRate { get; set; }
        public string BonusType { get; set; }
        public string BonusPromoCode { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal JackpotPayout { get; set; }
        public string TableId { get; set; }
        public string TransactionIds { get; set; }
        public DateTime ProviderTransactionDate { get; set; }
        public DateTime BetDate { get; set; }
        public DateTime SettlementDate { get; set; }
    }
}
