using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class TransferHistory
    {
        public long ParentUserId { get; set; }
        public long UserId { get; set; }
        public Guid ReferenceId { get; set; }
        public string Username { get; set; }
        public int UserTypeId { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PreBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Amount { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PostBalance { get; set; }
        public string CurrencyCode { get; set; }
        public bool IsCashPlayer { get; set; }
        public int TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime TransactionDateOffset { get; set; }
    }
}
