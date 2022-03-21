using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class FixtureScoreRequestDTO
    {
        [JsonPropertyName("fixtureId")]
        public string FixtureId { get; set; }

        [JsonPropertyName("score")]
        public string Score { get; set; }
    }

    public class FixtureScoreResponseDTO
    {
        [JsonPropertyName("statusId")]
        public int StatusId { get; set; }
    }
}
