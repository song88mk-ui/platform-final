using Platform.Shared.Enums;
using System;

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
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool IsCurrent { get; set; }
    }
}
