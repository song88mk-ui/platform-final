using Platform.Shared.Models;
using System;

namespace Platform.Shared.MessageModels
{
    public class OnESportCreditResponse
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
