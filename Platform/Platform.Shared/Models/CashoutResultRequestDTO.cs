using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class CashoutResultRequestDTO
    {
        public string ClientCode { get; set; }
        public string UserId { get; set; }
        public string BetId { get; set; }
        public string BetStatus { get; set; }
        public string CashOutStatus { get; set; }
        public decimal CashOutStake { get; set; }
        public string Message { get; set; }
    }

    public class CashoutResultResponseDTO
    {
        public int StatusId { get; set; }
    }
}
