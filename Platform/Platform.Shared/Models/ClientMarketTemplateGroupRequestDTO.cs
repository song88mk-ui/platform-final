using System;

namespace Platform.Shared.Models
{
    public class ClientMarketTemplateGroupRequestDTO
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
    }
}
