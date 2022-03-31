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
            Selections = new List<SelectionSettlementMessage>();
        }
        public int SportId { get; set; }
        public string ProviderFixtureId { get; set; }
        public string FixtureSpecifier { get; set; }
        public string HomeScore { get; set; }
        public string AwayScore { get; set; }
        public List<SelectionSettlementMessage> Selections { get; set; }
    }

    public class SelectionSettlementMessage
    {
        public string MarketTemplateId { get; set; }
        public string SelectionTemplateId { get; set; }
        public string MarketSpecifier { get; set; }
        public bool Result { get; set; }
        public VoidFactor? VoidFactor { get; set; }
        public decimal DeadHeatFactor { get; set; }
    }

    public class InternalSelectionSettlementMessagePayload
    {
        public long MarketId { get; set; }
        public long SelectionId { get; set; }
        public BetStatus SelectionStatus { get; set; }
        public decimal DeadHeatFactor { get; set; }
    }
}
