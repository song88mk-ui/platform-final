using System;

namespace Platform.Shared.Data.Reports.Entities
{
    public class RunningBalanceTransactionFinal
    {
        public long Id { get; set; }

        public long ParentUserId { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string CurrencyCode { get; set; }


        public decimal PreCashBalance { get; set; }

        public decimal CashBalance { get; set; }

        public decimal PostCashBalance { get; set; }


        public decimal PreTrueCashBalance { get; set; }

        public decimal TrueCashBalance { get; set; }

        public decimal PostTrueCashBalance { get; set; }


        public decimal PreOutstandingBalance { get; set; }

        public decimal OutstandingBalance { get; set; }

        public decimal PostOutstandingBalance { get; set; }


        public bool IsCashPlayer { get; set; }

        public string ReferenceId { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}
