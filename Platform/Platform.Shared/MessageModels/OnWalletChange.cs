namespace Platform.Shared.MessageModels
{
    public class OnWalletChange
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public decimal Balance { get; set; }
    }
}
