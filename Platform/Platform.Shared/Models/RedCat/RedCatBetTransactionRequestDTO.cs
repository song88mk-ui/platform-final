using System;

namespace Platform.Shared.Models.RedCat
{
    public class RedCatBetTransactionRequestDTO : RedCatBaseRequestDTO
    {
        public string Currency { get; set; }
        public string MemberCode { get; set; }
        public string? OperatorId { get; set; }
        public DateTime EventDate { get; set; }
        public long BetMarketId { get; set; }
        public decimal TransactionAmt { get; set; }
        public long TransactionId { get; set; }
        public long BetId { get; set; }
        public string GameId { get; set; }
    }
}
