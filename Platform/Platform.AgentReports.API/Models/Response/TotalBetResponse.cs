using System;
using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Response
{
    public class TotalBetResponse
    {
        public TotalBetResponse()
        {
            BetTotalDaily = new List<DailyBetTotal>();
            LeaderboardWon = new List<BetTotalLeaderboard>();
            LeaderboardLost = new List<BetTotalLeaderboard>();
            SportsBetTotal = new List<SportBetTotal>();
        }

        public int TotalStakeCount { get; set; }
        public decimal TotalStakeAmount { get; set; }
        public int TotalAgentCount { get; set; }
        public int TotalMemberCount { get; set; }
        public decimal TotalMemberWLC { get; set; }
        public decimal TotalAgentWLC { get; set; }
        public decimal TotalTurnover { get; set; }
        public decimal TotalMargin { get; set; }
        public decimal AverageStake { get; set; }
        public DateTime StartWeek { get; set; }
        public DateTime EndWeek { get; set; }
        public List<DailyBetTotal> BetTotalDaily { get; set; }
        public List<BetTotalLeaderboard> LeaderboardWon { get; set; }
        public List<BetTotalLeaderboard> LeaderboardLost { get; set; }
        public List<SportBetTotal> SportsBetTotal { get; set; }

        #region Nested Class

        public class DailyBetTotal
        {
            public string Day { get; set; }
            public int TotalStakeCount { get; set; }
            public decimal TotalStakeAmount { get; set; }
        }

        public class BetTotalLeaderboard
        {
            public string Username { get; set; }
            public decimal TotalAmount { get; set; }
            public string AgentName { get; set; }
        }

        public class SportBetTotal
        {
            public int SportId { get; set; }
            public string SportName { get; set; }
            public int TotalStakeCount { get; set; }
            public decimal TotalMemberWLC { get; set; }
            public decimal TotalAgentWLC { get; set; }
            public decimal TotalTurnover { get; set; }
            public decimal TotalMargin { get; set; }
        }

        #endregion

    }
}
