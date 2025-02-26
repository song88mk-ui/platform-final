using System;

namespace Platform.Shared.Models.RedCat
{
    public class RedCatOperatorBetTransactionRequest
    {
        public Guid BetId { get; set; }
        public string BetIdRef { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string OperatorToken { get; set; }
        public string Endpoint { get; set; }
        public string CurrencyCode { get; set; }
        public long BetMarketId { get; set; }
        public string GameId { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public decimal Payout { get; set; }
        public decimal Stake { get; set; }
        public bool IsAgent { get; set; }
        public DateTime BetDate { get; set; }
        public string CorrelationId { get; set; }
    }
}
