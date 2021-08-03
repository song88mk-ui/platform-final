using System;

namespace Platform.Shared.Data.Platform.Entities
{
    public class OperatorSettlementRetry
    {
        public Guid BetId { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string Endpoint { get; set; }
        public string MessageType { get; set; }
        public string Info { get; set; }
        public string OperatorToken { get; set; }
        public string CorrelationId { get; set; }
        public int RetryCount { get; set; }
        public bool IsRetry { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
