using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class WinLossSummary
    {
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

        public long UserId { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string CurrencyCode { get; set; }

        public int StakeCount { get; set; }
        public decimal StakeAmount { get; set; }

        public decimal ValidStakeAmount { get; set; }

        
        public decimal Commission { get; set; }

        public decimal MemberWinLoss { get; set; }

        public decimal MemberCommission { get; set; }

        public decimal MemberWLC { get; set; }

        public decimal AgentWinLoss { get; set; }

        public decimal AgentCommission { get; set; }

        public decimal AgentWLC { get; set; }

        public decimal DownlineWinLoss { get; set; }

        public decimal DownlineCommission { get; set; }

        public decimal DownlineWLC { get; set; }

        public decimal UplineWinLoss { get; set; }

        public decimal UplineCommission { get; set; }

        public decimal UplineWLC { get; set; }

        public decimal Transfer { get; set; }

        public int MemberCount { get; set; }
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

    }

}
