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
        public string[] GameTypes { get; set; }
        public string GameLaunchTarget { get; set; }
    }
}
