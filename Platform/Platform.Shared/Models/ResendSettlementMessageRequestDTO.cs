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

        // BO-125: r.bet.operator is bound by both Operator.Servicev2 and Provider.Service, while
        // r.bet.cancellation is bound by Provider.Service alone (ArgoCD apps/platform). Sending a cancelled
        // bet to the provider on r.bet.cancellation therefore does not send a second cancellation to the
        // operator.
        public const string OperatorRoutingKey = "r.bet.operator";
        public const string ProviderCancellationRoutingKey = "r.bet.cancellation";

        // Optional. Null or empty keeps the original destination, OperatorRoutingKey. Betting.API refuses
        // any key that is not one of the two above.
        public string RoutingKey { get; set; }
    }
}
