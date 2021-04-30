using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.API.Models
{
    public class ClientTopFixtureFilterRequest
    {
        public int ClientIdRef { get; set; }
        public int SportId { get; set; }
        public int LeagueId { get; set; }
        public long FixtureId { get; set; }
        public int SegmentId { get; set; }
        public SortDirection SortOrder { get; set; }
    }
}
