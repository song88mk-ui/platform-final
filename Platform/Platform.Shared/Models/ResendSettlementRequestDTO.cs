using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ResendSettlementRequestDTO
    {
        public ResendSettlementRequestDTO()
        {
            BetIds = new List<string>();
        }

        public List<string> BetIds { get; set; }
    }
}
