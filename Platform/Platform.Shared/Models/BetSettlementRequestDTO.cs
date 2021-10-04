using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class BetSettlementRequestDTO
    {
        public BetSettlementRequestDTO()
        {
            Selections = new List<InternalSelectionSettlementMessage>();
        }
        public string FixtureId { get; set; }
        public List<InternalSelectionSettlementMessage> Selections { get; set; }
    }
    public class InternalSelectionSettlementMessage
    {
        public string MarketId { get; set; }
        public string SelectionId { get; set; }
        [DefaultValue(1)]
        public decimal DeadHeatFactor { get; set; } = 1m;
        [JsonIgnore]
        public BetStatus SelectionStatus { get; set; }

    }
}
