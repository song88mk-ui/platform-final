using System;

namespace Platform.Shared.Models
{
    public class BetStatusRequestDTO
    {
        public Guid BetId { get; set; }
    }
    public class PartialCashoutStatusRequestDTO
    {
        public Guid TransactionId { get; set; }
    }
}
