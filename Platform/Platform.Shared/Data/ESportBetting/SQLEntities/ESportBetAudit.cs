using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.ESportBetting.SQLEntities
{
    public class ESportBetAudit
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public string ProviderTransactionId { get; set; }
        public string TransactionType { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Endpoint { get; set; }
        public string UserId { get; set; }
        public string OperatorToken { get; set; }
        public bool IsAgent { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Stake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal WinLoss { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Payout { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Reason { get; set; }
        public string CurrencyCode { get; set; }
        public string ProviderBetId { get; set; }
        public string ReferenceBetId { get; set; }
        public string ReferenceTransactionId { get; set; }
        public string Ticket { get; set; }
        public string Description { get; set; }
        public string Amount { get; set; }
        public DateTime ProviderTransactionDate { get; set; }
        public DateTime BetDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public string Payload { get; set; }
    }
}
