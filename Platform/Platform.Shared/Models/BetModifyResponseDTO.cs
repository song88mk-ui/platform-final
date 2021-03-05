using Platform.Shared.Enums;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetModifyResponseDTO
    {
        public List<BetModifyResponse> Data { get; set; }

        public BetModifyResponseDTO()
        {
            Data = new List<BetModifyResponse>();
        }
    }

    public class BetModifyResponse
    {
        public string BetId { get; set; }
        public string OldBetStatus { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class BetModifyRequestDTO
    {
        public List<string> BetIds { get; set; }
        public BetStatus NewStatus { get; set; }
        public string Reason { get; set; }
        public long? ModifiedByUserId { get; set; }

        public BetModifyRequestDTO()
        {
            BetIds = new List<string>();
        }
    }
}
