using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models.RedCat
{
    public class RedCatBetRequestDTO : RedCatBaseRequestDTO
    {
        public long BetId { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string Endpoint { get; set; }
        public string OperatorToken { get; set; }
        public decimal ExhangeRate { get; set; }
        public CurrencyCode Currency { get; set; }
        public string MemberCode { get; set; }
        public string? OperatorId { get; set; }
        public DateTime EventDate { get; set; }
        public long BetMarketId { get; set; }
        public decimal TransactionAmt { get; set; }
        public long TransactionId { get; set; }
        public string GameId { get; set; }
    }
}
