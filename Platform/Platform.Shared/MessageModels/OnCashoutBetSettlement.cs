using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnCashoutBetSettlement
    {
        public OnCashoutBetSettlement()
        {
            Probabilities = new List<CashedOutProbabilities>();
        }
        public Guid BetId { get; set; }
        public string UserId { get; set; }
        public string ClientId { get; set; }
        public CashoutType CashoutType { get; set; }
        public decimal CashOutStake { get; set; }
        public decimal TaxedStake { get; set; }
        public decimal TaxedWinnings { get; set; }
        public bool HasLive { get; set; }
        public List<CashedOutProbabilities> Probabilities { get; set; }
    }

    public class CashedOutProbabilities
    {
        public Guid BetSelectionId { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public double Probability { get; set; }
        public int CashOutStatusId { get; set; }
    }
}
