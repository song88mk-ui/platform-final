using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OperatorESportsCreditRequestDTO
    {
        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; }

        [JsonPropertyName("betId")]
        public string BetId { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        //[JsonPropertyName("odds")]
        //public decimal Odds { get; set; }

        [JsonPropertyName("betStatus")]
        public string BetStatus { get; set; }

        //[JsonPropertyName("stakeTax")]
        //public decimal StakeTax { get; set; }

        //[JsonPropertyName("winningsTax")]
        //public decimal WinningsTax { get; set; }

        //[JsonPropertyName("payout")]
        //public decimal Payout { get; set; }

        [JsonPropertyName("transactionDate")]
        public string TransactionDate { get; set; }
    }
}
