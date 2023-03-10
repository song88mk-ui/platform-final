using System;
using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Response
{
    public class TransferHistoryResponse
    {
        public TransferHistoryResponse()
        {
            Transfers = new List<Detail>();
        }

        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public List<Detail> Transfers { get; set; }

        #region Nested Class

        public class Detail
        {
            public long UserId { get; set; }
            public string Username { get; set; }
            public string CurrencyCode { get; set; }
            public DateTime TransactionDate { get; set; }
            public decimal TransferredAmount { get; set; }
            public decimal RemainingCashBalance { get; set; }
        }

        #endregion
    }
}
