using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{

    public class BetBuilderSelectionResponseDTO
    {
        public BetBuilderSelectionResponseDTO()
        {
            Markets = new List<BetBuilderMarketDTO>();
        }
        public List<BetBuilderMarketDTO> Markets { get; set; }
    }
}
