using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetHistoryRunningFilteredResponseDTO
    {
        public BetHistoryRunningFilteredResponseDTO()
        {
            Bets = new List<BetHistoryRunning>();
        }
        public List<BetHistoryRunning> Bets { get; set; }
    }

    public class BetHistoryRunning
    {
        public string BetId { get; set; }
        public int CashOutStatusId { get; set; }
        public string CashOutStatusName { get; set; }
        public decimal CashOutStake { get; set; }
    }
}
