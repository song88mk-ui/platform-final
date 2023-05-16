namespace Platform.Shared.Models
{
    public class GameLobbyRequestDTO
    {
        public string UserId { get; set; }
        public string CurrencyCode { get; set; }
        public string CountryCode { get; set; }
        public string LanguageCode { get; set; }
        public bool IsDemo { get; set; }
        public bool IsMobile { get; set; }
        public string OperatorToken { get; set; }
        public bool ShowBalance { get; set; }
        public bool ShowBetHistory { get; set; }
    }
}
