using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class BetResponseDTO
    {
        public string UserId { get; set; }
        public string BetId { get; set; }
        public BetStatus BetStatus { get; set; }
        public List<BetSelectionState> BetSelections { get; set; }
    }

    public class BetSelectionState
    {
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public string SelectionId { get; set; }
        public decimal Odds { get; set; }
        public string CorrelationId { get; set; }
    }
}
