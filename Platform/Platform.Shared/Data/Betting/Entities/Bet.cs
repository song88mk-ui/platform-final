using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Betting.Entities
{
    public class Bet
    {
        public Guid BetId { get; set; }
        public string UserId { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Stake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal TrueStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Odds { get; set; }
        public int OddsTypeId { get; set; }
        public string OddsTypeName { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Payout { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal WinLoss { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Reason { get; set; }
        public string CurrencyCode { get; set; }
        public string LanguageCode { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal ExchangeRate { get; set; }
        public string IPAddress { get; set; }
        public string Channel { get; set; }
        public string ClientId { get; set; }
        public int ClientIdRef { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string LimitId { get; set; }
        public string Endpoint { get; set; }
        public string OperatorToken { get; set; }
        public decimal CompanyShare { get; set; }
        public bool IsResettled { get; set; }
        public bool IsCashout { get; set; }
        public DateTime BetDate { get; set; }
        public DateTime BetDateOffset { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime SettlementDateOffset { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal StakeTax { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal WinningsTax { get; set; }
    }
}
