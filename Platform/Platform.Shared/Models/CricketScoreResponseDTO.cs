using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class CricketScoreResponseDTO
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("response_code")]
        public string ResponseCode { get; set; }
        [JsonPropertyName("score")]
        public Score Score { get; set; }
    }

    public class Score
    {
        [JsonPropertyName("bettingSuspended")]
        public bool BettingSuspended { get; set; }
        [JsonPropertyName("matchTitle")]
        public string MatchTitle { get; set; }
        [JsonPropertyName("currentInningsNumber")]
        public int CurrentInningsNumber { get; set; }
        [JsonPropertyName("innings")]
        public List<Innings> Innings { get; set; }
    }

    public class Innings
    {
        [JsonPropertyName("inningsNumber")]
        public int InningsNumber { get; set; }
        [JsonPropertyName("teamName")]
        public string TeamName { get; set; }
        [JsonPropertyName("runs")]
        public decimal Runs { get; set; }
        [JsonPropertyName("wickets")]
        public decimal Wickets { get; set; }
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
        [JsonPropertyName("overs")]
        public decimal Overs { get; set; }
        [JsonPropertyName("isFinalInnings")]
        public bool IsFinalInnings { get; set; }
    }
}
