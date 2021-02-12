using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetConfigRequestDTO
    {
        public int ClientId { get; set; }
        public string UserId { get; set; }
        public string CurrencyCode { get; set; }
        public string LanguageCode { get; set; }
        public List<BetConfigSelectionDTO> BetSelections { get; set; }
        public BetConfigCurrentSelectionDTO CurrentSelection { get; set; }
    }

    public class BetConfigCurrentSelectionDTO
    {
        public long FixtureId { get; set; }
        public decimal Odds { get; set; }
        public long SelectionId { get; set; }
    }

    public class BetConfigSelectionDTO
    {
        public long FixtureId { get; set; }
        public long SelectionId { get; set; }
        public decimal Odds { get; set; }
    }

}
