using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetBuilderSelectionsOddsResponseDTO
    {

        public BetBuilderSelectionsOddsResponseDTO()
        {
            Selections = new List<BetSelectionOddsRequest>();
        }
        public List<BetSelectionOddsRequest> Selections { get; set; }
        public decimal Odds { get; set; }
        public decimal Probability { get; set; }
    }
}
