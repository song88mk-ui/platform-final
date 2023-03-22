using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Response
{
    public class PaymentTransferListResponse
    {
        public PaymentTransferListResponse()
        {
            Transfers = new List<Detail>();
        }

        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public decimal TotalYesterdayCashBalance { get; set; }
        public decimal TotalTodayCashBalance { get; set; }
        public decimal TotalOutstandingBet { get; set; }
        public decimal TotalCashBalance { get; set; }
        public decimal TotalTransferAmount { get; set; }
        public string UserIds { get; set; }
        public List<Detail> Transfers { get; set; }

        #region Nested Class

        public class Detail
        {
            public long UserId { get; set; }
            public int UserTypeId { get; set; }
            public string Username { get; set; }
            public decimal YesterdayCashBalance { get; set; }
            public decimal TodayCashBalance { get; set; }
            public decimal OutstandingBet { get; set; }
            public decimal CashBalance { get; set; }
            public decimal TransferAmount { get; set; }
        }

        #endregion
    }
}

