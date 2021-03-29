using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Platform.Shared.Data.Betting.Entities
{
   public class StakeLimitFixture
    {
        public string UserId { get; set; }

        public long FixtureId { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Remaining { get; set; }
    }
}
