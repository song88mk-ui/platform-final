using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class Bet
    {
        public long ParentUserId { get; set; }

        public Guid BetId { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }

        public long AgentId { get; set; }

        public string AgentUsername { get; set; }

        public int UserTypeId { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PositionTaking { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Commission { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Stake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal TrueStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Odds { get; set; }

        public int OddsTypeId { get; set; }

        public string OddsTypeName { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Payout { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal WinLoss { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; }

        public string Reason { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal ExchangeRate { get; set; }

        public string CurrencyCode { get; set; }

        public string IPAddress { get; set; }

        public int Count { get; set; }

        public int SportId { get; set; }

        public string SportName { get; set; }

        public int SegmentId { get; set; }

        public string SegmentName { get; set; }

        public int LeagueId { get; set; }

        public string LeagueName { get; set; }

        public long FixtureId { get; set; }

        public string FixtureName { get; set; }

        public int FixtureStatusId { get; set; }

        public string FixtureStatusName { get; set; }

        public DateTime FixtureStartDate { get; set; }

        public string Score { get; set; }

        public string MarketTemplateGroupId { get; set; }

        public string MarketTemplateGroupName { get; set; }

        public string MarketTemplateId { get; set; }

        public long MarketId { get; set; }

        public string MarketName { get; set; }

        public string Line { get; set; }

        public string SelectionTemplateId { get; set; }

        public long SelectionId { get; set; }

        public string SelectionName { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal ReceivePositionTaking { get; set; }

        public int ProviderBrandId { get; set; }
        public string ProviderBrandName { get; set; }

        public int GameId { get; set; }
        public string GameName { get; set; }

        public string CasinoDetails { get; set; }

        public int? GameProvider { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTime TransactionDateOffset { get; set; }

        public string MarketTypeId { get; set; }

        public string MarketGroupId { get; set; }
    }
}
