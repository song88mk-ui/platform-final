using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
  public  class BetSettlementRequestDTO
    {
        public BetSettlementRequestDTO()
        {
            Selections = new List<InternalSelectionSettlementMessage>();
        }
        public long FixtureId { get; set; }
        public List<InternalSelectionSettlementMessage> Selections { get; set; }
    }
    public class InternalSelectionSettlementMessage
    {
        public long MarketId { get; set; }
        public long SelectionId { get; set; }
        public SettlementStatus SettlementStatus { get; set; }
    }
}
