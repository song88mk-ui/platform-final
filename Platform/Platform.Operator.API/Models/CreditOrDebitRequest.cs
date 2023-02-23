using System;

namespace Platform.Operator.API.Models
{
    public class CreditOrDebitRequest
    {
        public Guid ClientId { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; }
        public string ReferenceId { get; set; }
    }
}
