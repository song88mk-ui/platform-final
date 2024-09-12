using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.MessageModels
{
    public class OnClientTopMarketBetPlacement
    {
        public string ClientId { get; set; }
        public string ClientCode { get; set; }  
        public string ClientName { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public long FixtureId { get; set; }
        public string FixtureName { get; set; }
        public int FixtureTypeId { get; set; }
        public string FixtureTypeName { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        public int MarketSelectionCount { get; set; }
        public string CurrencyCode { get; set; }
    }
}
