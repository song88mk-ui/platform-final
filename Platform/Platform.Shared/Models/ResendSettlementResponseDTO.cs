using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ResendSettlementResponseDTO
    {
        public ResendSettlementResponseDTO()
        {
            Data = new List<ResendSettlementResponse>();
        }

        public List<ResendSettlementResponse> Data { get; set; }
    }

    public class ResendSettlementResponse
    {
        public Guid BetId { get; set; }
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }

}
