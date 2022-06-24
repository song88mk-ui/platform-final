using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetSelectionDetailsResponseDTO
    {
        public BetSelectionDetailsResponseDTO()
        {
            Selections = new List<BetSelectionDetailsDTO>();
        }
        public List<BetSelectionDetailsDTO> Selections { get; set; }
    }
}
