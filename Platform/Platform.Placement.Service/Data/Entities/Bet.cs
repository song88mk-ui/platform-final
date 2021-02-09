using System;

namespace Platform.Placement.Service.Entities
{
    public class Bet
    {
        public Guid BetId { get; set; }
        public string UserId { get; set; }
        public decimal Stake { get; set; }
        public decimal TrueStake { get; set; }
        public decimal Odds { get; set; }
        public int OddsTypeId { get; set; }
        public string OddsTypeName { get; set; }
        public decimal Payout { get; set; }
        public decimal WinLoss { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Reason { get; set; }
        public string CurrencyCode { get; set; }
        public string LanguageCode { get; set; }
        public decimal ExchangeRate { get; set; }
        public string IPAddress { get; set; }
        public string Channel { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public string Endpoint { get; set; }
        public decimal CompanyShare { get; set; }
        public bool IsResettled { get; set; }
        public bool IsCashout { get; set; }
        public DateTime BetDate { get; set; }
        public DateTime BetDateOffset { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime SettlementDateOffset { get; set; }
    }
}
