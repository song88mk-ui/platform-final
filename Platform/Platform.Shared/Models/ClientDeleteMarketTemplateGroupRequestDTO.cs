using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientDeleteMarketTemplateGroupRequestDTO
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public ClientMarketTemplateGroupModel MarketTemplateGroup { get; set; }
    }
}
