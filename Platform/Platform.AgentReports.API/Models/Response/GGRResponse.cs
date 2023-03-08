using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Response
{
    public class GGRResponse
    {
        public GGRResponse()
        {
            Bets = new List<GGRModelResponse>();
        }

        public int CurrentPage { get; set; }

        public int PageCount { get; set; }

        public int PageSize { get; set; }

        public int RowCount { get; set; }

        public decimal TotalStakeAmount { get; set; }

        public int TotalStakeCount { get; set; }

        public decimal TotalPayout { get; set; }

        public decimal TotalGGR { get; set; }

        public int TotalMemberCount { get; set; }

        public List<GGRModelResponse> Bets { get; set; }
    }

    public class GGRDetailsResponse
    {
        public GGRDetailsResponse()
        {
            Bets = new List<GGRDetailsModelResponse>();
        }

        public int CurrentPage { get; set; }

        public int PageCount { get; set; }

        public int PageSize { get; set; }

        public int RowCount { get; set; }

        public decimal TotalStakeAmount { get; set; }

        public int TotalStakeCount { get; set; }

        public decimal TotalPayout { get; set; }

        public decimal TotalGGR { get; set; }

        public List<GGRDetailsModelResponse> Bets { get; set; }
    }
}
