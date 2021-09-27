using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class BetSettlementRequestPayloadDTO
    {
        public BetSettlementRequestPayloadDTO()
        {
            Selections = new List<InternalSelectionSettlementMessagePayload>();
        }
        public long FixtureId { get; set; }
        public List<InternalSelectionSettlementMessagePayload> Selections { get; set; }
    }
    public class InternalSelectionSettlementMessagePayload
    {
        public long MarketId { get; set; }
        public long SelectionId { get; set; }
        public BetStatus SettlementStatus { get; set; }
        public decimal DeadHeatFactor { get; set; }
    }
}
