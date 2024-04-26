using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OGTransactionResponseDTO : OGBaseResponseDTO
    {
        [JsonPropertyName("transaction_status")]
        public string TransactionStatus { get; set; }
    }
}
