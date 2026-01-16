using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Platform.Entities
{
    public class Operator
    {
        public Guid UserId { get; set; }
        public string OperatorToken { get; set; }
        public Guid ClientId { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
