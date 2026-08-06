namespace Platform.Shared.Models.Provider.GTS
{
    public class GtsCashout
    {
        // Amounts are in the bet's currency; GTSRepository converts to EUR on send.
        // Per GTS Back-End Integration doc these must be cumulative across all cashouts of the bet.
        public decimal CashedOutTotalStake { get; set; }
        public decimal CashedOutAmount { get; set; }
    }
}
