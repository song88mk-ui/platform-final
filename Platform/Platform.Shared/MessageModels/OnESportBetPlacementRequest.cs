using Platform.Shared.Models;
using System;

namespace Platform.Shared.MessageModels
{
    public class OnESportBetPlacementRequest
    {
        public string TransactionId { get; set; }
        public Guid BetId { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string Endpoint { get; set; }
        public string OperatorToken { get; set; }
        public OperatorESportsBetPlacementRequestDTO Payload { get; set; }
    }

    public class OnESportBetSettlementRequest
    {
        public string TransactionId { get; set; }
        public Guid BetId { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string Endpoint { get; set; }
        public string OperatorToken { get; set; }
        public OperatorESportsBetSettlementRequestDTO Payload { get; set; }
    }

    public class OnESportBetSettlementResponse
    {
        public string TransactionId { get; set; }
        public Guid BetId { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string Endpoint { get; set; }
        public string OperatorToken { get; set; }
        public OperatorBetResponseDTO Payload { get; set; }
    }
}
