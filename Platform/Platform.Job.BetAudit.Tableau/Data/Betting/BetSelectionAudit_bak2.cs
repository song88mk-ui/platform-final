using System;

namespace Platform.Job.BetAudit.Tableau.Data.Betting
{
    public class BetSelectionAudit_bak2
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public Guid BetSelectionId { get; set; }
        public int BetSelectionStatusId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
