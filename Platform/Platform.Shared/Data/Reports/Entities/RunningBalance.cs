using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class RunningBalance
    {
        public long ParentUserId { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string CurrencyCode { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal CashBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal TrueCashBalance { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal OutstandingBalance { get; set; }

        public bool IsCashPlayer { get; set; }
    }
}
