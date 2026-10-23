using Platform.Shared.Models;

namespace Platform.RedCat.API.Models
{
    public class WalletBalanceRequest : RedCatBaseRequest
    {
        public string Currency { get; set; }
        public string MemberCode { get; set; }
        public string OperatorId { get; set; }
    }
}
