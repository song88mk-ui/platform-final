using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Sportsbook.SQLEntities
{
    [Table("SelectorMarketTemplate2")]
    public class SelectorMarketTemplate
    {
        public int SportId { get; set; }
        public string MarketTemplateId { get; set; }
        public string MarketTemplateCode { get; set; }
        public int SortOrder { get; set; }
    }
}
