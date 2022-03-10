using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ResendSettlementMessageRequestDTO
    {
        public ResendSettlementMessageRequestDTO()
        {
            Messages = new List<ResendSettlementMessageDTO>();
        }

        public List<ResendSettlementMessageDTO> Messages { get; set; }
    }

    public class ResendSettlementMessageDTO
    {
        public Guid BetId { get; set; }
        public string MessageType { get; set; }
        public string Info { get; set; }
        public string CorrelationId { get; set; }
    }
}
