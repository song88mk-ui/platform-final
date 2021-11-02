using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class CashoutStateRequestDTO
    {
        public Guid BetId { get; set; }
    }

    public class CashoutStateResponseDTO
    {
        public CashoutStateResponseDTO()
        {
            Probabilities = new List<CashedOutProbabilities>();
        }
        public Guid BetId { get; set; }
        public CashoutType CashoutType { get; set; }
        public decimal CashOutStake { get; set; }
        public decimal TaxedStake { get; set; }
        public decimal TaxedWinnings { get; set; }
        public decimal NonTaxedPayout { get; set; }
        public List<CashedOutProbabilities> Probabilities { get; set; }
    }
}
