using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.RedCatBetting.Entities
{
    public class BetAudit
    {
        public long TransactionId { get; set; }
        public int Id { get; set; }
        public Guid BetId { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string Endpoint { get; set; }
        public string MemberCode { get; set; }
        public string? OperatorId { get; set; }
        public string OperatorToken { get; set; }
        public string CurrencyCode { get; set; }
        public string LanguageCode { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal ExchangeRate { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Reason { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Amount { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Payout { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal WinLoss { get; set; }
        public decimal CompanyShare { get; set; }
        public bool IsResettled { get; set; }
        public string BetIdRef { get; set; }
        public DateTime BetDate { get; set; }
        public DateTime BetDateOffset { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime SettlementDateOffset { get; set; }
        public long? RollbackTransactionId { get; set; } = null;
        public DateTime Timestamp { get; set; }
    }
}
