using Platform.Shared.Enums;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ResendSettlementRequestDTO
    {
        public ResendSettlementRequestDTO()
        {
            Bets = new List<ResendSettlementReques>();
        }

        public List<ResendSettlementReques> Bets { get; set; }
    }

    public class ResendSettlementReques
    {
        public string BetId { get; set; }
        public BetStatus CurrentStatus { get; set; }
    }
}
