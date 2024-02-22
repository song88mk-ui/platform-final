using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class CashoutBetRequestDTO
    {
        public string APIKey { get; set; }
        public string OperatorToken { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int ClientIdRef { get; set; }
        public string LimitId { get; set; }
        public string Endpoint { get; set; }
        public decimal CompanyShare { get; set; }
        public Guid BetId { get; set; }
        public string UserId { get; set; }
        public decimal CashOutStake { get; set; }
        public decimal CashOutStakePercent { get; set; }
    }
}
