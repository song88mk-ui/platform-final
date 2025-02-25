using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace Platform.Shared.Data.RedCatBetting.Entities
{
    public class Bet
    {
        public Guid BetId { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string Endpoint { get; set; }
        public string OperatorToken { get; set; }
        public string CurrencyCode { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string GameId { get; set; }
        public long BetMarketId { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Stake { get; set; }
        public string BetIdRef { get; set; }
        public DateTime BetDate { get; set; }
    }
}
