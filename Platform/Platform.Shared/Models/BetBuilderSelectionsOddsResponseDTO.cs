using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetBuilderSelectionsOddsResponseDTO
    {
        public decimal TotalOdds { get; set; }
    }

    public class BetBuilderMarketSelectionResponseDTO : BetBuilderSelectionsOddsResponseDTO
    {
        public BetBuilderMarketSelectionResponseDTO()
        {
            Markets = new List<BetBuilderMarketDTO>();
        }
        public List<BetBuilderMarketDTO> Markets { get; set; }
    }
}
