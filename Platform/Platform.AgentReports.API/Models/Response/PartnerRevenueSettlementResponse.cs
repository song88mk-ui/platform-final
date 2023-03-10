using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Response
{
    public class PartnerRevenueSettlementResponse
    {
        public PartnerRevenueSettlementResponse()
        {
            RevenueSettlementDetails = new List<Detail>();
        }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public int RowCount { get; set; }
        public string TotalUnsettledAmount { get; set; }
        public string TotalSettlementAmount { get; set; }
        public List<Detail> RevenueSettlementDetails { get; set; }

        #region Nested Class

        public class Detail
        {
            public long UserId { get; set; }
            public string UserName { get; set; }
            public string UnsettledAmount { get; set; }
            public string CurrentMonthBalance { get; set; }
            public string SettlementAmount { get; set; }
        }

        #endregion
    }
}

