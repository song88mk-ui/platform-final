using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class MTSConfigRequestDTO
    {
        public MTSConfigRequestDTO()
        {
            BetSelections = new List<MTSConfigSelectionRequestDTO>();
        }
        public string UserId { get; set; }
        public string Currency { get; set; }
        public string DeviceId { get; set; }
        public string Language { get; set; }
        public int LimitId { get; set; }
        public List<MTSConfigSelectionRequestDTO> BetSelections { get; set; }
    }

    public class MTSConfigSelectionRequestDTO
    {
        public string FixtureSpecifier { get; set; }
        public string SelectionSpecifier { get; set; }
        public decimal Odds { get; set; }
        public int SportId { get; set; }
    }
}
