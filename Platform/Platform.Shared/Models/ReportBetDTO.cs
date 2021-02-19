using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class ReportBetDTO
    {
        public Guid BetId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public int ApplicationId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public decimal CompanyShare { get; set; }
        public decimal ClientShare { get; set; }
        public decimal GGR { get; set; }
        public decimal Stake { get; set; }
        public decimal TrueStake { get; set; }
        public decimal Odds { get; set; }
        public int OddsTypeId { get; set; }
        public string OddsTypeName { get; set; }
        public decimal Payout { get; set; }
        public decimal WinLoss { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public string Reason { get; set; }
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
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
        public int MarketTemplateGroupId { get; set; }
        public string MarketTemplateGroupName { get; set; }
        public int MarketTemplateId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public decimal Line { get; set; }
        public int SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
        public string SelectionName { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime TransactionDateOffset { get; set; }
        public DateTime? SettlementDate { get; set; }
        public DateTime? SettlementDateOffset { get; set; }
        public int MarketTypeId { get; set; }
    }
}
