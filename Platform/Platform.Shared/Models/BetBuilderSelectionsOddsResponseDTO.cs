using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetBuilderSelectionsOddsResponseDTO
    {

        public BetBuilderSelectionsOddsResponseDTO()
        {
            Markets = new List<BetBuilderMarketDTO>();
        }
        public List<BetBuilderMarketDTO> Markets { get; set; }
        public decimal Odds { get; set; }
        public decimal Probability { get; set; }
    }
}
