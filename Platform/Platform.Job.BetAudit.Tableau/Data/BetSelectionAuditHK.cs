using System;

namespace Platform.Job.BetAudit.Tableau.Data
{
    public class BetSelectionAuditHK
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public Guid BetSelectionId { get; set; }
        public int BetSelectionSequence { get; set; }
        public bool IsLastBetSelection { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
