using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Data.Betting.Entities
{
    public class ClientTopMarket
    {
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
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
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        public int BetCount { get; set; }
    }
}
