using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.RedCatBetting.Entities
{
    public class BetAudit
    {
        public int Id { get; set; }
        public long TransactionId { get; set; }
        public Guid BetId { get; set; }
        public string CLientId { get; set; }
        public string ClientCode { get; set; }
        public string Endpoint { get; set; }
        public string MemberCode { get; set; }
        public string? OperatorId { get; set; }
        public string OperatorToken { get; set; }
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Reason { get; set; }
        public string GameId { get; set; }
        public long BetMarketId { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Amount { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Payout { get; set; }
        public string BetIdRef { get; set; }
        public long? RollbackTransactionId { get; set; } = null;
        public DateTime BetDate { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
