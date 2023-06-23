using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Platform.Entities
{
    public class OperatorAudit
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OperatorToken { get; set; }
        public Guid ClientId { get; set; }
        public Guid BetId { get; set; }
        public int TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PreBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Amount { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PostBalance { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public string TransactionId { get; set; }
    }
}
