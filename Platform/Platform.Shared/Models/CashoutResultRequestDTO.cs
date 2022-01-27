using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class CashoutResultRequestDTO
    {
        [JsonPropertyName("clientCode")]
        public string ClientCode { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("betId")]
        public string BetId { get; set; }

        [JsonPropertyName("betStatus")]
        public string BetStatus { get; set; }

        [JsonPropertyName("cashOutStatus")]
        public string CashOutStatus { get; set; }

        [JsonPropertyName("cashOutStake")]
        public decimal CashOutStake { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class CashoutResultResponseDTO
    {
        [JsonPropertyName("statusId")]
        public int StatusId { get; set; }
    }
}
