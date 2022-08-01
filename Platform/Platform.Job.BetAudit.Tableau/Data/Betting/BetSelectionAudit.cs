using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Job.BetAudit.Tableau.Data.Betting
{
    public class BetSelectionAudit
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public Guid BetSelectionId { get; set; }
        public int BetSelectionStatusId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
