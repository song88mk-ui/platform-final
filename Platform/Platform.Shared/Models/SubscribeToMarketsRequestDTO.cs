using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class MarketsSubscriptionRequestDTO
    {
        string FixtureId { get;set; }
        List<string> MarketIds { get; set; }
    }
}
