using System;
using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Response
{
    public class PartnerRevenueSettlementHistoryResponse
    {
        public PartnerRevenueSettlementHistoryResponse()
        {
            RevenueSettlementHistoryDetails = new List<Detail>();
        }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public int RowCount { get; set; }
        public List<Detail> RevenueSettlementHistoryDetails { get; set; }

        #region Nested Class

        public class Detail
        {
            public DateTime SettlementDate { get; set; }
            public string UserName { get; set; }
            public decimal SettledAmount { get; set; }
            public string MonthSettled { get; set; }
            public string SettledBy { get; set; }
        }

        #endregion
    }
}

