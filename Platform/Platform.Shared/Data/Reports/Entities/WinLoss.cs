using System;

namespace Platform.Shared.Data.Reports.Entities
{
    public class WinLoss
    {
        public long ParentUserId { get; set; }

        public long DirectUserId { get; set; }

        public long IndirectUserId { get; set; }

        public Guid BetId { get; set; }

        public DateTime TransactionDate { get; set; }

        public decimal ExchangeRate { get; set; }

        public string AgentUsername { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string CurrencyCode { get; set; }

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

        public bool IsParlay { get; set; }

        public decimal Stake { get; set; }

        public decimal TrueStake { get; set; }

        public decimal Odds { get; set; }

        public int OddsTypeId { get; set; }

        public string OddsTypeName { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

        public int SportId { get; set; }

        public string SportName { get; set; }

        public int SegmentId { get; set; }

        public string SegmentName { get; set; }

        public int LeagueId { get; set; }

        public string LeagueName { get; set; }

        public long FixtureId { get; set; }

        public string FixtureName { get; set; }

        public DateTime FixtureStartDate { get; set; }

        public string MarketTemplateId { get; set; }

        public string MarketName { get; set; }

        public string SelectionTemplateId { get; set; }

        public long SelectionId { get; set; }

        public string SelectionName { get; set; }

        public string PositionTakingDetails { get; set; }

        public string CommissionDetails { get; set; }

        public string Score { get; set; }

        public int FixtureStatusId { get; set; }

        public string FixtureStatusName { get; set; }

        public string Line { get; set; }

        public string MarketTemplateGroupId { get; set; }

        public string MarketTemplateGroupName { get; set; }

        public long MarketId { get; set; }

        public DateTime SettledDate { get; set; }

        public DateTime SettledDateOffset { get; set; }

        public DateTime TransactionDateOffset { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int ProviderBrandId { get; set; }

        public string ProviderBrandName { get; set; }

        public int GameId { get; set; }

        public string GameName { get; set; }

        public int GameProvider { get; set; }

        public string CasinoDetails { get; set; }

        public string ReferenceId { get; set; }

        public string MarketTypeId { get; set; }

        public string MarketGroupId { get; set; }

        public decimal? MaxLiability { get; set; }

        public bool? IsGGREnabled { get; set; }

        public decimal? GGR { get; set; }

        public decimal? Payout { get; set; }
    }
}
