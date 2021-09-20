
using Platform.Shared.Data.Betting.Entities;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class GetMultibetBonusRequestDTO
    {
        public int ClientIdRef { get; set; }
        public Guid ClientId { get; set; }
        public int NoOfGames { get; set; }
    }
    public class MultibetBonusRequestDTO
    {
        public int ClientIdRef { get; set; }
        public Guid ClientId { get; set; }
        public List<MultibetBonus> MultibetBonus { get; set; }
    }

}
