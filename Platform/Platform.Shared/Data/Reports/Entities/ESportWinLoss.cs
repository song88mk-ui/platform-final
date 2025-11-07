using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class ESportWinLoss
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

        public string ClientCode { get; set; }

        public string ClientName { get; set; }

        public string Endpoint { get; set; }

        public string UserId { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string OperatorToken { get; set; }

        public bool IsAgent { get; set; }

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

        [Column(TypeName = "decimal(26,12)")]
        public decimal Payout { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal Odds { get; set; }

        public int BetStatusId { get; set; }

        public string BetStatusName { get; set; }

        public string Reason { get; set; }

        public string CurrencyCode { get; set; }

        public string Ticket { get; set; }

        public string ProviderBetId { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal ReceivePositionTaking { get; set; }

        public string TransactionIds { get; set; }

        public DateTime ProviderTransactionDate { get; set; }

        public DateTime TransactionDateOffset { get; set; }

        public DateTime SettlementDate { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal MaxLiability { get; set; }

        public bool IsGGREnabled { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal GGR { get; set; }

        public string PositionTakingDetails { get; set; }

    }
}
