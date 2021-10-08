
using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ParlayBonusResponseDTO
    {
        public ParlayBonusResponseDTO()
        {
            ParlayBonusConfig = new List<ParlayBonusConfigResponse>();
        }
        public decimal ParlayBonusMinOdds { get; set; }
        public List<ParlayBonusConfigResponse> ParlayBonusConfig { get; set; }
    }

    public class ParlayBonusConfigResponse
    {
        public ParlayBonusConfigResponse()
        {
            ParlayBonusHistory = new List<ParlayBonusHistory>();
        }
        public int SelectionCount { get; set; }
        public decimal ParlayBonus { get; set; }
        public List<ParlayBonusHistory> ParlayBonusHistory { get; set; }
    }

    public class ParlayBonusHistory
    {
        public string LastUpdate { get; set; }
        public decimal ParlayBonus { get; set; }
    }

}
