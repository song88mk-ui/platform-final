using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Data.Betting.Entities
{
   public class StakeLimitFixture
    {
        public string UserId { get; set; }

        public long FixtureId { get; set; }

        public decimal Remaining { get; set; }
    }
}
