using Platform.Shared.Models;
using System;

namespace Platform.AgentReports.API.Models.Response
{
    public class GGRModelResponseDTO
    {
        public long UserId { get; set; }

        public string Username { get; set; }

        public bool IsAgent { get; set; }

        public string CurrencyCode { get; set; }

        public decimal StakeAmount { get; set; }

        public int StakeCount { get; set; }

        public decimal Payout { get; set; }

        public decimal GGR { get; set; }

        public int MemberCount { get; set; }
    }

    public class GGRDetailsModelResponse
    {
        public long UserId { get; set; }

        public Guid BetId { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTime SettlementDate { get; set; }

        public string Username { get; set; }

        public int UserTypeId { get; set; }

        public string CurrencyCode { get; set; }

        public decimal StakeAmount { get; set; }

        public decimal Payout { get; set; }

        public decimal GGR { get; set; }

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

        public CasinoDetailDTO CasinoDetails { get; set; }
        public string Reason { get; set; }

        public string BetScore { get; set; }

        public string FinalScore { get; set; }

        public string HalftimeScore { get; set; }

        public string MarketTypeId { get; set; }

        public string MarketGroupId { get; set; }

    }
}
