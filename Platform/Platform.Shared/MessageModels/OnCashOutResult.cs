using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.MessageModels
{
    public class OnCashOutResult 
    {
        public string BetId { get; set; }
        public BetStatus BetStatus { get; set; }
        public decimal CashOutStake { get; set; }
        public CashOutStatus CashOutStatus { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }

        public string Endpoint { get; set; }
    }
}
