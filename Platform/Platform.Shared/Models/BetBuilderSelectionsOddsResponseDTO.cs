using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BetBuilderSelectionsOddsResponseDTO : BetBuilderFixture
    {

    }

    public class BetBuilderFixture
    {
        public BetBuilderFixture()
        {
            Markets = new List<BetBuilderMarket>();
        }
        public string FixtureId { get; set; }
        public decimal TotalOdds { get; set; }
        public List<BetBuilderMarket> Markets { get; set; }
    }

    public class BetBuilderMarket
    {
        public BetBuilderMarket()
        {
            Selections = new List<BetBuilderSelection>();
        }
        public string MarketId { get; set; }
        public List<BetBuilderSelection> Selections { get; set; }
    }

    public class BetBuilderSelection
    {
        public string SelectionId { get; set; }
        public bool IsActive { get; set; }
    }
}
