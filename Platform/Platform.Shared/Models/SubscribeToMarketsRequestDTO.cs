using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class SubscribeToMarketsRequestDTO
    {
        string FixtureId { get;set; }
        List<string> MarketIds { get; set; }
    }
}
