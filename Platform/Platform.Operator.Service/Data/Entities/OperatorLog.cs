using System;

namespace Platform.Operator.Service.Data.Entities
{
    public class OperatorLog
    {
        public long Id { get; set; }
        public string Direction { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string URL { get; set; }
        public string BetId { get; set; }
        public string Info { get; set; }
        public string OperatorToken { get; set; }
        public string CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
