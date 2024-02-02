using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class MarketsSubscriptionRequestDTO
    {
        public string FixtureId { get;set; }
        public List<string> MarketIds { get; set; }
    }
}
