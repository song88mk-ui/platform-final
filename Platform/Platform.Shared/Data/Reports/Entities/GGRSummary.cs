
namespace Platform.Shared.Data.Reports.Entities
{
    public class GGRSummary
    {
        public int CurrentPage { get; set; }

        public int PageCount { get; set; }

        public int PageSize { get; set; }

        public int RowCount { get; set; }

        public decimal TotalStakeAmount { get; set; }

        public int TotalStakeCount { get; set; }

        public decimal TotalPayout { get; set; }

        public decimal TotalGGR { get; set; }

        public int TotalMemberCount { get; set; }

        public long UserId { get; set; }

        public string Username { get; set; }

        public bool IsAgent { get; set; }

        public string CurrencyCode { get; set; }

        public decimal StakeAmount { get; set; }

        public int StakeCount { get; set; }

        public decimal Payout { get; set; }

        public decimal GGR { get; set; }

        public int MemberCount { get; set; }

    }

}
