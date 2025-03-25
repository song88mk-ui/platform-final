using System.Collections.Generic;

namespace Platform.Shared.Models.RedCat
{
    public class OperatorRedCatBaseRequest
    {
        public OperatorRedCatBaseRequest()
        {
            BetSelections = new List<OperatorRedCatBetSelectionDTO>();
        }
        public string BetId { get; set; }
        public string UserId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public decimal StakeTax { get; set; }
        public decimal WinningsTax { get; set; }
        public decimal ParlayBonus { get; set; }
        public string BonusId { get; set; }
        public string BetStatus { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionId { get; set; }
        public List<OperatorRedCatBetSelectionDTO> BetSelections { get; set; }
    }
    public class OperatorRedCatBetSelectionDTO
    {
        public string BetSelectionId { get; set; }
        public string GameId { get; set; }
        public long MarketId { get; set; }
        public string BetSelectionStatus { get; set; }
    }
    public class OperatorRedCatBetRequestDTO : OperatorRedCatBaseRequest
    {
    }

    public class OperatorRedCatBetSettlementRequestDTO : OperatorRedCatBaseRequest
    {
        public decimal Payout { get; set; }
    }
}
