using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class BetModelRequestDTO : ReportBetDTO
    {
        public IEnumerable<ReportBetSelectionDTO> BetSelections { get; set; }
    }
}
