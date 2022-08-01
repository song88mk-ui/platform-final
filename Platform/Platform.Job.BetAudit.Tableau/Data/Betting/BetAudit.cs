using System;

namespace Platform.Job.BetAudit.Tableau.Data.Betting
{
    public class BetAudit
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public int BetStatusId { get; set; }
        public DateTime BetDate { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
