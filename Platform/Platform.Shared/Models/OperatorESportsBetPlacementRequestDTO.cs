using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class OperatorESportsBetPlacementRequestDTO
    {
        public string BetId { get; set; }
        public decimal Stake { get; set; }
        public string CurrencyCode { get; set; }
    }
}
