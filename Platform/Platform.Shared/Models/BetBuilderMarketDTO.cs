using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetBuilderMarketDTO
    {
        public BetBuilderMarketDTO()
        {
            Selections = new List<BetBuilderSelectionDTO>();
        }
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public bool IsCashout { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        public List<BetBuilderSelectionDTO> Selections { get; set; }
    }

    public class BetBuilderSelectionDTO
    {
        public string SelectionTemplateId { get; set; }
        public string SelectionId { get; set; }
        public string SelectionName { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string CorrelationId { get; set; }
    }
}
