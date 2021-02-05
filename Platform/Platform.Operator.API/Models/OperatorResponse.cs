using System;

namespace Platform.Operator.API.Models
{
    public class OperatorResponse
    {
        public Guid UserId { get; set; }
        public string OperatorToken { get; set; }
        public string ClientId { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; }
    }
}
