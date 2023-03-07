using System;

namespace Platform.Shared.Data.Reports.Entities
{
    public class CashBalanceTrackerFinal
    {
        public long ParentUserId { get; set; }

        public long UserId { get; set; }

        public Guid BetId { get; set; }

        public int UserTypeId { get; set; }

        public decimal Amount { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}
