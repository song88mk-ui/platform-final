using Platform.Shared.Enums;
using System.Collections.Generic;

namespace Platform.API.Models
{
    public class CashoutBetRequest
    {
        public string BetId { get; set; }
        public Channel Channel { get; set; }
        public string IPAddress { get; set; }
        public decimal Cashout { get; set; }
        public List<CashoutBetSelectionRequest> BetSelectionIds { get; set; }
    }

    public class CashoutBetSelectionRequest
    {
        public string BetSelectionId { get; set; }
    }
}
