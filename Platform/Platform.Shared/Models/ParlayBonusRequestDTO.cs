
using Platform.Shared.Data.Betting.Entities;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class GetParlayBonusRequestDTO
    {
        public int ClientIdRef { get; set; }
        public Guid ClientId { get; set; }
        public int NoOfSelection { get; set; }
    }
    public class ParlayBonusRequestDTO
    {
        public int ClientIdRef { get; set; }
        public Guid ClientId { get; set; }

        public decimal MinOdds { get; set; }
        public List<ParlayBonus> ParlayBonus { get; set; }
    }

}
