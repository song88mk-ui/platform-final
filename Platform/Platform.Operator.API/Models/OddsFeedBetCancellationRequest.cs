using System.Collections.Generic;
using System;

namespace Platform.Operator.API.Models
{
    public class OddsFeedBetCancellationRequest
    {
        public OddsFeedBetCancellationRequest()
        {
            BetSelections = new List<BetSelection>();
        }
        public string BetId { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorMessage { get; set; }
        public List<BetSelection> BetSelections { get; set; }

        public class BetSelection
        {
            public string MarketTemplateId { get; set; }
            public string SelectionTemplateId { get; set; }
            public decimal Odds { get; set; }
            public string FixtureSpecifier { get; set; }
            public string MarketSpecifier { get; set; }
            public string SelectionSpecifier { get; set; }
        }
    }
}
