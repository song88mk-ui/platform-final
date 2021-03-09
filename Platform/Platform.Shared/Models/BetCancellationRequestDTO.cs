using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class BetCancellationRequestDTO
    {
        public BetCancellationRequestDTO()
        {
            Markets = new List<InternalMarketCancellationMessage>();
        }
        public string FixtureId { get; set; }
        public List<InternalMarketCancellationMessage> Markets { get; set; }
    }
    public class InternalMarketCancellationMessage
    {
        public string MarketId { get; set; }
        public string Reason { get; set; }

        [JsonIgnore]
        public SettlementStatus SettlementStatus { get; set; }
    }
}
