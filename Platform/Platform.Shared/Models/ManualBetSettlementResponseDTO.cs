using Platform.Shared.Enums;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ManualBetSettlementResponseDTO
    {
        public List<ManualBetResponse> Data { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }

        public ManualBetSettlementResponseDTO()
        {
            Data = new List<ManualBetResponse>();
        }
    }

    public class ManualBetResponse
    {
        public ManualBetDetailsDTO BetDetails { get; set; }
        public string OldBetStatus { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class ManualBetSettlementRequest
    {
        public List<ManualBetRequest> BetData { get; set; }
        public long? ModifiedByUserId { get; set; }

        public ManualBetSettlementRequest()
        {
            BetData = new List<ManualBetRequest>();
        }
    }

    public class ManualBetRequest
    {
        public string BetId { get; set; }
        public BetStatus NewStatus { get; set; }
        public string Reason { get; set; }
    }

}
