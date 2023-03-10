using System;

namespace Platform.Shared.Data.Reports.Entities
{
    public class SettlementHistory
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string CurrencyCode { get; set; }
        public string Coverage { get; set; }
        public decimal Balance { get; set; }
        public bool IsSettled { get; set; }
        public decimal? AmountSettled { get; set; }
        public DateTime? DateSettled { get; set; }
        public long? SettledByUserId { get; set; }
        public string SettledBy { get; set; }
    }
}
