using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class OutstandingBetFixtureResponseDTO
    {
        public OutstandingBetFixtureResponseDTO()
        {

        }

        public int CurrentPage { get; set; }

        public int PageCount { get; set; }

        public int PageSize { get; set; }

        public int RowCount { get; set; }

        public List<BetFixtureModel> Bets { get; set; }

        #region Nested Class
        public class BetFixtureModel
        {
            public int SportId { get; set; }

            public string SportName { get; set; }

            public int SegmentId { get; set; }

            public string SegmentName { get; set; }

            public int LeagueId { get; set; }

            public string LeagueName { get; set; }

            public long FixtureId { get; set; }

            public string FixtureName { get; set; }

            public DateTime FixtureStartDate { get; set; }

            public List<BetFixtureMarketModel> Markets { get; set; }
        }

        public class BetFixtureMarketModel
        {
            public string MarketTemplateGroupId { get; set; }

            public string MarketTemplateGroupName { get; set; }

            public string MarketTemplateId { get; set; }

            public string MarketName { get; set; }

            public decimal Receive { get; set; }
        }
        #endregion
    }
}

