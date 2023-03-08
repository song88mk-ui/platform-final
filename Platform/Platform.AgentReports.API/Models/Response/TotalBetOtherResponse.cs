using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Response
{
    public class TotalBetOtherResponse
    {
        public TotalBetOtherResponse()
        {
            ProductBetTotal = new List<TotalBetOther>();

        }

        public List<TotalBetOther> ProductBetTotal { get; set; }

        #region Nested Class

        public class TotalBetOther
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int TotalStakeCount { get; set; }
            public decimal TotalMemberWLC { get; set; }
            public decimal TotalAgentWLC { get; set; }
            public decimal TotalTurnover { get; set; }
            public decimal TotalMargin { get; set; }
        }

        #endregion
    }
}
