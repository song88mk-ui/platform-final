using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetFixtureRequestDTO
    {
        public BetFixtureRequestDTO()
        {
            Selections = new List<BetFixtureSelectionRequestDTO>();
        }
        public List<BetFixtureSelectionRequestDTO> Selections { get; set; }
    }

    public class BetFixtureSelectionRequestDTO
    {
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public long MarketId { get; set; }
        public long SelectionId { get; set; }
        public decimal Odds { get; set; }
        public string CorrelationId { get; set; }
    }
}
