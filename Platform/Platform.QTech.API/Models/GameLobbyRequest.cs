namespace Platform.QTech.API.Models
{
    public class GameLobbyRequest
    {
        public string Token { get; set; }
        public string PlayerId { get; set; }
        public string Currency { get; set; }
        public string Country { get; set; }
        public string Lang { get; set; }
        public string Mode { get; set; }
        public string Device { get; set; }
        public string WalletSessionId { get; set; }
        public bool Balance { get; set; }
        public bool GameHistory { get; set; }
    }
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
