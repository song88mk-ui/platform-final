using System.Collections.Generic;
using System;

namespace Platform.Operator.API.Models
{
    public class OddsFeedBetRequest
    {
        public OddsFeedBetRequest()
        {
            BetSelections = new List<OddsFeedBetSelectionRequest>();
        }
        public string BetId { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Reason { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public List<OddsFeedBetSelectionRequest> BetSelections { get; set; } = new List<BetSelectionRequest>();
    }

    public class OddsFeedBetSelectionRequest
    {
        public string MarketTemplateId { get; set; }
        public string SelectionTemplateId { get; set; }
        public decimal Odds { get; set; }
        public string FixtureSpecifier { get; set; }
        public string MarketSpecifier { get; set; }
        public string SelectionSpecifier { get; set; }
    }
}
