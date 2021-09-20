
using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class MultibetBonusResponseDTO
    {
        public MultibetBonusResponseDTO()
        {
            MultibetBonus = new List<MultibetBonus>();
        }
        public List<MultibetBonus> MultibetBonus { get; set; }
    }

    public class MultibetBonusAuditResponseDTO
    {
        public MultibetBonusAuditResponseDTO()
        {
            MultibetBonusAudit = new List<MultibetBonusAudit>();
        }
        public List<MultibetBonusAudit> MultibetBonusAudit { get; set; }
    }

}
