using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class CasinoBetHistoryResponseDTO
    {
        public CasinoBetHistoryResponseDTO()
        {
            Bets = new List<CasinoBetHistoryDTO>();
        }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int RowCount { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal TotalWinLoss { get; set; }
        public decimal TotalWLC { get; set; }
        public List<CasinoBetHistoryDTO> Bets { get; set; }
    }

    public class CasinoBetHistoryDTO
    {
        public string BetId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ProductName { get; set; }
        public string GameName { get; set; }
        public string GameType { get; set; }
        public string ProviderName { get; set; }
        public string ProviderBetId { get; set; }
        public decimal Stake { get; set; }
        public decimal Payout { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
    }
}
