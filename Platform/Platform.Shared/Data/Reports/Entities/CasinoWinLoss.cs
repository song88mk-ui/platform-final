using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class CasinoWinLoss
    {
        public long ParentUserId { get; set; }

        public long DirectUserId { get; set; }

        public long IndirectUserId { get; set; }

        public Guid BetId { get; set; }

        public DateTime BetDate { get; set; }

        public string ProviderTransactionId { get; set; }

        public long AgentId { get; set; }

        public string AgentUsername { get; set; }

        public string TransactionType { get; set; }

        public string ClientId { get; set; }

        public string UserId { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal PositionTaking { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal Commission { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal MemberWinLoss { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal MemberCommission { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal MemberWLC { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal DownlineWinLoss { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal DownlineCommission { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal DownlineWLC { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal AgentWinLoss { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal AgentCommission { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal AgentWLC { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal UplineWinLoss { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal UplineCommission { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal UplineWLC { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal Stake { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal StakeBonus { get; set; }

        public int BetStatusId { get; set; }

        public string BetStatusName { get; set; }

        public string CurrencyCode { get; set; }

        public string GameId { get; set; }

        public string Device { get; set; }

        public string ClientType { get; set; }

        public string ProviderBetId { get; set; }

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

        public DateTime TransactionDateOffset { get; set; }

        public DateTime SettledDate { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal MaxLiability { get; set; }

        public bool IsGGREnabled { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal GGR { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal Payout { get; set; }
    }
}
