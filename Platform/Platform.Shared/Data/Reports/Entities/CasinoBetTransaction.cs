using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class CasinoBetTransaction
    {
        public long Id { get; set; }

        public long ParentUserId { get; set; }

        public long AgentId { get; set; }

        public string AgentUsername { get; set; }

        public Guid TransactionId { get; set; }

        public string ProviderTransactionId { get; set; }

        public string TransactionType { get; set; }

        public string ClientId { get; set; }

        public string UserId { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal PositionTaking { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal Commission { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal Stake { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal StakeBonus { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal Payout { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal WinLoss { get; set; }

        public int BetStatusId { get; set; }

        public string BetStatusName { get; set; }

        public string CurrencyCode { get; set; }

        public string GameId { get; set; }

        public string Device { get; set; }

        public string ClientType { get; set; }

        public string ClientRoundId { get; set; }

        public string Category { get; set; }

        public bool IsComplete { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal ExchangeRate { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal ReceivePositionTaking { get; set; }

        public string JackpotContributions { get; set; }

        public string ConversionRate { get; set; }

        public string BonusType { get; set; }

        public string BonusPromoCode { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal JackpotPayout { get; set; }

        public string TableId { get; set; }

        public string TransactionIds { get; set; }

        public DateTime ProviderTransactionDate { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTime TransactionDateOffset { get; set; }

        public DateTime SettlementDate { get; set; }
    }
}
