using Platform.Shared.Enums;
using System;

namespace Platform.API.Models
{
    public class SettlementHistoryRequest
    {
        public string UserId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
