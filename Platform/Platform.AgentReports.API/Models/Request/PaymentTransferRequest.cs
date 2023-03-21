using System;

namespace Platform.AgentReports.API.Models.Request
{
    public class PaymentTransferRequest
    {
        public long ParentAgentId { get; set; }
        public long UserId { get; set; }
        public decimal TransferAmount { get; set; }
        public Guid ReferenceId { get; set; }
        public decimal OriginalCreditLimit { get; set; }
        public string Username { get; set; }
        public string CurrencyCode { get; set; }
        public bool IsCashMarket { get; set; }
        public string ClientId { get; set; }
    }
}
