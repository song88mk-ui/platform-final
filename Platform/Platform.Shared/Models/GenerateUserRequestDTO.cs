using Platform.Shared.Data.Platform.Entities;
using System;

namespace Platform.Shared.Models
{
    public class GenerateUserRequestDTO
    {
        public Guid ClientId { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; }
    }
}
