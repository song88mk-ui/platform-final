
using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ParlayBonusResponseDTO
    {
        public ParlayBonusResponseDTO()
        {
            ParlayBonus = new List<ParlayBonus>();
        }
        public decimal MinOdds { get; set; }
        public List<ParlayBonus> ParlayBonus { get; set; }
    }

    public class ParlayBonusAuditResponseDTO
    {
        public ParlayBonusAuditResponseDTO()
        {
            ParlayBonusAudit = new List<ParlayBonusAudit>();
        }
        public List<ParlayBonusAudit> ParlayBonusAudit { get; set; }
    }

}
