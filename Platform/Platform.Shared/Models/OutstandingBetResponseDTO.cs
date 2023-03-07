using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class OutstandingBetResponseDTO
    {
        public OutstandingBetResponseDTO()
        {
            Bets = new List<Bet>();
        }

        public int CurrentPage { get; set; }

        public int PageCount { get; set; }

        public int PageSize { get; set; }

        public int RowCount { get; set; }

        public decimal TotalStake { get; set; }

        public decimal TotalReceive { get; set; }

        public List<Bet> Bets { get; set; }

        #region Nested Class

        public class Bet
        {
            public Bet()
            {
                BetSelections = new List<BetSelection>();
            }

            public long ParentUserId { get; set; }
            public Guid BetId { get; set; }
            public long UserId { get; set; }
            public string Username { get; set; }
            public decimal PositionTaking { get; set; }
            public decimal Commission { get; set; }
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
            public decimal ExchangeRate { get; set; }
            public string CurrencyCode { get; set; }
            public string IPAddress { get; set; }
            public DateTime TransactionDate { get; set; }
            public string FixtureName { get; set; }
            public decimal Receive { get; set; }
            public List<BetSelection> BetSelections { get; set; }
            public string MarketTemplateId { get; set; }
            public string MarketName { get; set; }
            public string SelectionTemplateId { get; set; }
            public string SelectionName { get; set; }
            public string MarketTypeId { get; set; }
        }

        public class BetSelection
        {
            public long ParentUserId { get; set; }
            public Guid BetId { get; set; }
            public Guid BetSelectionId { get; set; }
            public long UserId { get; set; }
            public int SportId { get; set; }
            public string SportName { get; set; }
            public int SegmentId { get; set; }
            public string SegmentName { get; set; }
            public int LeagueId { get; set; }
            public string LeagueName { get; set; }
            public long FixtureId { get; set; }
            public string FixtureName { get; set; }
            public long MarketId { get; set; }
            public string MarketName { get; set; }
            public long SelectionId { get; set; }
            public string SelectionName { get; set; }
            public int FixtureStatusId { get; set; }
            public string FixtureStatusName { get; set; }
            public string Line { get; set; }
            public decimal Odds { get; set; }
            public int StatusId { get; set; }
            public string StatusName { get; set; }
            public bool IsSettled { get; set; }
            public bool IsResettled { get; set; }
            public DateTime FixtureStartDate { get; set; }
            public string MarketTemplateGroupId { get; set; }
            public string MarketTemplateGroupName { get; set; }
            public string MarketTemplateId { get; set; }
            public string SelectionTemplateId { get; set; }
            public string Score { get; set; }
            public string BetScore { get; set; }
            public string FinalScore { get; set; }
            public int? MarketTypeId { get; set; }
        }

        #endregion
    }
}

