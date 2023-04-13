using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class CashBalanceTracker
    {
        public long ParentUserId { get; set; }

        public long UserId { get; set; }

        public Guid BetId { get; set; }

        public int UserTypeId { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Amount { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}
