using System;
using System.Collections.Generic;

namespace Platform.AgentReports.API.Models
{
    public class WinLossModel
    {
        public long UserId { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string CurrencyCode { get; set; }

        public decimal StakeAmount { get; set; }

        public decimal ValidStakeAmount { get; set; }

        public decimal StakeCount { get; set; }

        public decimal Commission { get; set; }

        public decimal MemberWinLoss { get; set; }

        public decimal MemberCommission { get; set; }

        public decimal MemberWLC { get; set; }

        public decimal DownlineWinLoss { get; set; }

        public decimal DownlineCommission { get; set; }

        public decimal DownlineWLC { get; set; }

        public decimal AgentWinLoss { get; set; }

        public decimal AgentCommission { get; set; }

        public decimal AgentWLC { get; set; }

        public decimal UplineWinLoss { get; set; }

        public decimal UplineCommission { get; set; }

        public decimal UplineWLC { get; set; }

        public decimal Transfer { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

        public int MemberCount { get; set; }
    }
}
