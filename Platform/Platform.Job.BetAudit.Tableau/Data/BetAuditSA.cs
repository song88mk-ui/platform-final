using System;

namespace Platform.Job.BetAudit.Tableau.Data
{
    public class BetAuditSA
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public int BetSequence { get; set; }
        public bool IsParlay { get; set; }
        public bool IsLastBet { get; set; }
        public int BetStatusId { get; set; }
        public DateTime Timestamp { get; set; }
        public int? PreviousBetStatusId { get; set; }
        public DateTime? PreviousTimestamp { get; set; }
    }
}
