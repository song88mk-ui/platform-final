using System;

namespace Platform.Shared.Data.Entities
{
    public class Operator
    {
        public Guid UserId { get; set; }
        public Guid OperatorToken { get; set; }
        public Guid ClientId { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
