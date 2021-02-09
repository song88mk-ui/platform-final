using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class OperatorBetRequestDTO : BetRequestDTO
    {
        public Guid TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
