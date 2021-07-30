using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetSelectionProbabilityRequestDTO
    {
        public List<Tuple<long, long>> Selections { get; set; }
    }
}
