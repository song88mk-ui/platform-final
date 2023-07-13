namespace Platform.Shared.MessageModels
{
    public class OnPayableChange
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public decimal Payable { get; set; }
    }
}
