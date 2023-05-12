namespace Platform.QTech.API.Models
{
    public class TransactionRequest 
    {
        public string WalletSession { get; set; }
        public string PassKey { get; set; }
        public string TxnType { get; set; }
        public string TxnId { get; set; }
        public string PlayerId { get; set; }
        public string RoundId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public decimal BonusBetAmount { get; set; }
        public string BonusType { get; set; }
        public string BonusPromoCode { get; set; }
        public JackpotContribution[] JpContributions { get; set; }
        public decimal JpPayout { get; set; }
        public ConversionRate ConversionRate { get; set; } = new ConversionRate();
        public string GameId { get; set; }
        public string Device { get; set; }
        public string ClientType { get; set; }
        public string ClientRoundId { get; set; }
        public string Category { get; set; }
        public string Created { get; set; }
        public string Completed { get; set; }
        public string TableId { get; set; }
    }
}
