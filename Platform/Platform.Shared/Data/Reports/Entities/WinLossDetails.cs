
using System;
using System.Collections.Generic;

namespace Platform.Shared.Data.Reports.Entities
{
    public class WinLossDetails
    {
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

        
        public long UserId { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string CurrencyCode { get; set; }

        public Guid BetId { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTime SettlementDate { get; set; }

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

        public bool IsParlay { get; set; }

        public decimal Odds { get; set; }

        public int OddsTypeId { get; set; }

        public string OddsTypeName { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

        public string VirtualSportName { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int LeagueId { get; set; }

        public string LeagueName { get; set; }

        public long FixtureId { get; set; }

        public string FixtureName { get; set; }

        public DateTime FixtureStartDate { get; set; }

        public string MarketTemplateId { get; set; }

        public string MarketName { get; set; }

        public string SelectionTemplateId { get; set; }

        public string SelectionName { get; set; }

        public string Score { get; set; }

        public int FixtureStatusId { get; set; }

        public string FixtureStatusName { get; set; }

        public string Line { get; set; }

        public string MarketTemplateGroupId { get; set; }

        public string MarketTemplateGroupName { get; set; }

        public string PositionTakingDetails { get; set; }

        public string CommissionDetails { get; set; }
        
        public string CasinoDetails { get; set; }

        public string Reason { get; set; }

        public string MarketTypeId { get; set; }

        public string MarketGroupId { get; set; }
    }

    public class PositionTakingDetails
    {
        public decimal Upline { get; set; }

        public decimal Agent { get; set; }

        public decimal Downline { get; set; }
    }

    public class CommissionDetails
    {
        public string Username { get; set; }

        public decimal Commission { get; set; }
    }

}
