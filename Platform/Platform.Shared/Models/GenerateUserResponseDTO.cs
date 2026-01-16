using Platform.Shared.Data.Platform.Entities;
using System;

namespace Platform.Shared.Models
{
    public class GenerateUserResponseDTO
    {
        public Guid ClientId { get; set; }
        public Guid UserId { get; set; }
        public string OperatorToken { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; }
    }
}
