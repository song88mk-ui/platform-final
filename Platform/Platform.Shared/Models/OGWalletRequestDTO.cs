namespace Platform.Shared.Models
{
    public class OGWalletRequestDTO
    {
        public string player_code { get; set; }
        public long timestamp { get; set; }
        public string signature { get; set; }
    }
}
