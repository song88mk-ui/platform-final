using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.Shared.Data.Betting.Entities
{
    public class Tier
    {
        [Key]
        public int TierId { get; set; }
        public decimal MaxStake { get; set; }
        public decimal MaxPerGame { get; set; }
    }
}
