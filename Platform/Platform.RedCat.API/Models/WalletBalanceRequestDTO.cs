namespace Platform.RedCat.API.Models
{
    public class WalletBalanceRequestDTO : BaseRequest
    {
        public string Currency { get; set; }
        public string MemberCode { get; set; }
        public string? OperatorId { get; set; }
    }
}
