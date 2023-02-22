namespace Platform.Wallet.API.Models
{
    public class CreditOrDebitRequest
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public int TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; }
        public string ReferenceId { get; set; }
    }
}
