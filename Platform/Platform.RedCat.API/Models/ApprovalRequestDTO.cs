namespace Platform.RedCat.API.Models
{
    public class ApprovalRequestDTO : BaseRequest
    {
        public string Currency { get; set; }
        public string MemberCode { get; set; }
        public string? OperatorId { get; set; }
        public decimal TransactionAmt { get; set; }
    }
}
