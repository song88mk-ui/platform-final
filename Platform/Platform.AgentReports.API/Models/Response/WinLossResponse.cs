using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Response
{
    public class WinLossResponse
    {
        public WinLossResponse()
        {
            Bets = new List<WinLossModel>();
        }

        public int CurrentPage { get; set; }

        public int PageCount { get; set; }

        public int PageSize { get; set; }

        public int RowCount { get; set; }

        public decimal TotalStakeAmount { get; set; }

        public decimal TotalValidStakeAmount { get; set; }

        public int TotalStakeCount { get; set; }

        public decimal TotalCommission { get; set; }

        public decimal TotalMemberWinLoss { get; set; }

        public decimal TotalMemberCommission { get; set; }

        public decimal TotalMemberWLC { get; set; }

        public decimal TotalDownlineWinLoss { get; set; }

        public decimal TotalDownlineCommission { get; set; }

        public decimal TotalDownlineWLC { get; set; }

        public decimal TotalAgentWinLoss { get; set; }

        public decimal TotalAgentCommission { get; set; }

        public decimal TotalAgentWLC { get; set; }

        public decimal TotalUplineWinLoss { get; set; }

        public decimal TotalUplineCommission { get; set; }

        public decimal TotalUplineWLC { get; set; }

        public decimal TotalTransfer { get; set; }

        public int TotalMemberCount { get; set; }

        public List<WinLossModel> Bets { get; set; }
    }

    public class WinLossDetailsResponse
    {
        public WinLossDetailsResponse()
        {
            Bets = new List<WinLossDetailsModel>();
        }

        public int CurrentPage { get; set; }

        public int PageCount { get; set; }

        public int PageSize { get; set; }

        public int RowCount { get; set; }

        public decimal TotalStakeAmount { get; set; }

        public decimal TotalValidStakeAmount { get; set; }

        public decimal TotalCommission { get; set; }

        public decimal TotalMemberWinLoss { get; set; }

        public decimal TotalMemberCommission { get; set; }

        public decimal TotalMemberWLC { get; set; }

        public decimal TotalDownlineWinLoss { get; set; }

        public decimal TotalDownlineCommission { get; set; }

        public decimal TotalDownlineWLC { get; set; }

        public decimal TotalAgentWinLoss { get; set; }

        public decimal TotalAgentCommission { get; set; }

        public decimal TotalAgentWLC { get; set; }

        public decimal TotalUplineWinLoss { get; set; }

        public decimal TotalUplineCommission { get; set; }

        public decimal TotalUplineWLC { get; set; }

        public List<WinLossDetailsModel> Bets { get; set; }
    }
}
