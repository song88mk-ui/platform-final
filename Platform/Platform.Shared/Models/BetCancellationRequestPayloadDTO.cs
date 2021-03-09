using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class BetCancellationRequestPayloadDTO
    {
        public BetCancellationRequestPayloadDTO()
        {
            Markets = new List<InternalMarketCancellationMessagePayload>();
        }
        public long FixtureId { get; set; }
        public List<InternalMarketCancellationMessagePayload> Markets { get; set; }
    }
    public class InternalMarketCancellationMessagePayload
    {
        public long MarketId { get; set; }
        public string Reason { get; set; }
        public SettlementStatus SettlementStatus { get; set; }
    }
}
