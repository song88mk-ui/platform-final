using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetBuilderSelectionsOddsRequestDTO
    {
        public BetBuilderSelectionsOddsRequestDTO()
        {
            Selections = new List<BetSelectionOddsRequest>();
        }
        public List<BetSelectionOddsRequest> Selections { get; set; }
        public string ClientCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }

    public class BetSelectionOddsRequest
    {
        public int SportId { get; set; }
        public string FixtureId { get; set; }
        public string MarketId { get; set; }
        public string SelectionId { get; set; }
        public string CorrelationId { get; set; }
    }
}
