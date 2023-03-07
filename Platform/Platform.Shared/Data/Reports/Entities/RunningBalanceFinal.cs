namespace Platform.Shared.Data.Reports.Entities
{
    public class RunningBalanceFinal
    {
        public long ParentUserId { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string CurrencyCode { get; set; }

        public decimal CashBalance { get; set; }

        public decimal TrueCashBalance { get; set; }

        public decimal OutstandingBalance { get; set; }

        public bool IsCashPlayer { get; set; }
    }
}
