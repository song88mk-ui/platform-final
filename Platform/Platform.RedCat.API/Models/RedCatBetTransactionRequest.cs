namespace Platform.RedCat.API.Models
{
    public class RedCatBetTransactionRequest : RedCatBaseRequest
    {
        public string Currency { get; set; }
        public string MemberCode { get; set; }
        public string OperatorId { get; set; }
        public string EventDate { get; set; }
        public string BetMarketId { get; set; }
        public string TransactionAmt { get; set; }
        public string TransactionId { get; set; }
        public string BetId { get; set; }
        public string GameId { get; set; }
    }
}
