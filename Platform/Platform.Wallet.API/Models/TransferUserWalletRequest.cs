using System;

namespace Platform.Wallet.API.Models
{
    public class TransferUserWalletRequest
    {
        public Guid ClientId { get; set; }
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; }
        public string ReferenceId { get; set; }
    }
}
