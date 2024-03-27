using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetHistoryRequestDTO
    {
        public string APIKey { get; set; }
        public string OperatorToken { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public BetStatusFilter BetStatusFilter { get; set; }
        public CasinoBetStatusFilter CasinoBetStatusFilter { get; set; }
        public ProductFilter ProductFilter { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsOddsString { get; set; }
        public string BetId { get; set; } = string.Empty;
        public List<int> BetType { get; set; } = new List<int>();
        public List<int> BetStatus { get; set; } = new List<int>();
    }
}
