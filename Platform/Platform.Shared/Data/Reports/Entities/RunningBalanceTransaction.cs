using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class RunningBalanceTransaction
    {
        public long Id { get; set; }

        public long ParentUserId { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string CurrencyCode { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal PreCashBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal CashBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PostCashBalance { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal PreTrueCashBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal TrueCashBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PostTrueCashBalance { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal PreOutstandingBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal OutstandingBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PostOutstandingBalance { get; set; }


        public bool IsCashPlayer { get; set; }

        public string ReferenceId { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}
