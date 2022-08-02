using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientCopyMarketTemplateGroupRequestDTO
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public int FixtureStatusId { get; set; }
        public Guid CopyToClientId { get; set; }
        public int CopyToSportId { get; set; }
        public int CopyToFixtureStatusId { get; set; }
    }
}
