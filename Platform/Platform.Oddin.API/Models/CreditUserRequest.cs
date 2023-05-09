namespace Platform.Oddin.API.Models
{
    public class CreditUserRequest
    {
        public CreditUserRequest()
        {
            Amount = new TicketAmount();
            Ticket = new Ticket();
        }
        public string Token { get; set; }
        public string UserId { get; set; }
        public string Currency { get; set; }
        public TicketAmount Amount { get; set; }
        public string Description { get; set; }
        public string TransactionId { get; set; }
        public string DebitOperatorTransactionId { get; set; }
        public string DebitTransactionId { get; set; }
        public string TicketId { get; set; }
        public int TransactionType { get; set; }
        public Ticket Ticket { get; set; }
    }
}
