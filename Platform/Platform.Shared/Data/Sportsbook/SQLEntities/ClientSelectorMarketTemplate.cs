using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    [Table("ClientSelectorMarketTemplate2")]
    public class ClientSelectorMarketTemplate
    {
        public Guid ClientId { get; set; }
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketTemplateCode { get; set; }
        public int AsiaSortOrder { get; set; }
        public int EuroSortOrder { get; set; }
        public string Identifier { get; set; }
    }
}
