using System;

namespace Platform.Shared.Data.Platform.Entities
{
    public class ESportOperatorLog
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
