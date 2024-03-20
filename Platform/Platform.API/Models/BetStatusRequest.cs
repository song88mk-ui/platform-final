using System;

namespace Platform.API.Models
{
    public class BetStatusRequest
    {
        public string BetId { get; set; }
    }
    public class PartialCashoutStatusRequest
    {
        public string TransactionId { get; set; }
    }
}
