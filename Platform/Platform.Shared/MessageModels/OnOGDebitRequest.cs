using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Platform.Shared.MessageModels
{
    public class OnOGDebitRequest
    {
        [JsonPropertyName("player_code")]
        public string PlayerCode { get; set; }
        [JsonPropertyName("timestamp")]
        public long TimeStamp { get; set; }
        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }
        [JsonPropertyName("transaction_type")]
        public string TransactionType { get; set; }
        [JsonPropertyName("game_code")]
        public string GameCode { get; set; }
        [JsonPropertyName("round_id")]
        public string RoundId { get; set; }
        [JsonPropertyName("currency")]
        public string Currency { get; set; }
        [JsonPropertyName("called_at")]
        public long CalledAt { get; set; }
        [JsonPropertyName("signature")]
        public string Signature { get; set; }
        [JsonPropertyName("records")]
        public object Records { get; set; }
    }

    public class OGRecord
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
        [JsonPropertyName("secondary_info")]
        public SecondaryInfo SecondaryInfo { get; set; }
        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; }
        [JsonPropertyName("other_info")]
        public OtherInfo OtherInfo { get; set; }
    }

    public class SecondaryInfo
    {
        [JsonPropertyName("bet_amount")]
        public double BetAmount { get; set; }
    }
    public class OtherInfo
    {

        [JsonPropertyName("bet_status")]
        public string BetStatus { get; set; }
    }
}
