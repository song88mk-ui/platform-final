using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.API.Models
{
    public class BetHistoryRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public BetStatusFilter BetStatusFilter { get; set; }
        public CasinoBetStatusFilter CasinoBetStatusFilter { get; set; } = CasinoBetStatusFilter.All;
        public ProductFilter ProductFilter { get; set; } = ProductFilter.Sportbook;
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool IsCurrent { get; set; }
        public string BetId { get; set; } = string.Empty;
        public List<int> BetType { get; set; } = new List<int>();
        public List<int> BetStatus { get; set; } = new List<int>();
    }
}
