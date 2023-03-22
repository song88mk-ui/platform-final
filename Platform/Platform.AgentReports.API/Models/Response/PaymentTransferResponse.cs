using System;

namespace Platform.AgentReports.API.Models.Response
{
    public class PaymentTransferResponse
    {
        public long UserId { get; set; }
        public decimal TransferAmount { get; set; }
        public bool IsFullTransfer { get; set; }
        public string ReferenceId { get; set; }
    }
}

