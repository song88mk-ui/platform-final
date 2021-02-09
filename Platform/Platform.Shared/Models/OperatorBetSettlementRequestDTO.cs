using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class OperatorBetSettlementRequestDTO
    {
        public Guid TransactionId { get; set; }
        public Guid BetId { get; set; }
        public string UserId { get; set; }
        public decimal Payout { get; set; }
        public string CurrencyCode { get; set; }
        public string BetStatus { get; set; }
        public DateTime TransactionDate { get; set; }
        public IEnumerable<BetSelectionRequestDTO> BetSelections { get; set; }
    }
}
