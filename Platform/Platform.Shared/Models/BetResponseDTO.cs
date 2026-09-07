using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Models
{
    public class BetResponseDTO
    {
        public string UserId { get; set; }
        public string BetId { get; set; }
        public BetStatus BetStatus { get; set; }
        public decimal Stake { get; set; }
        public decimal TotalOdds { get; set; }
        public List<BetSelectionState> BetSelections { get; set; }
    }

    public class BetSelectionState
    {
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public string SelectionId { get; set; }
        public decimal DecimalOdds { get; set; }
        public decimal MalaysianOdds { get; set; }
        public decimal HongkongOdds { get; set; }
        public decimal IndonesianOdds { get; set; }
        public string CorrelationId { get; set; }
    }

    public class BetCashOutResponseDTO
    {
        public string UserId { get; set; }
        public string BetId { get; set; }
        public string TransactionId { get; set; }
        public BetStatus BetStatus { get; set; }
        public decimal CashOutStake { get; set; }
    }

    public class CashOutResponse
    {
        public Guid BetId { get; set; }
        public decimal CashOutStake { get; set; }
        public decimal TaxedStake { get; set; }
        public decimal TaxedWinnings { get; set; }
        public List<Probabilities> Probabilities { get; set; }
    }

    public class CashOutDetailsReponse
    {
        public Guid BetId { get; set; }
        public CashOutStatus CashOutStatus { get; set; }
        public decimal CashOutStake { get; set; }
        public decimal TaxedStake { get; set; }
        public decimal TaxedWinnings { get; set; }
        public decimal NonTaxedPayout { get; set; }
        public bool HasLive { get; set; } = false;
        public List<Probabilities> Probabilities { get; set; }
    }

    public class Probabilities
    {
        public Guid BetSelectionId { get; set; }
        // Whether the selection's fixture is live at quote time — the stored
        // BetSelection.FixtureStatusId is placement-time and goes stale.
        public bool IsLive { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public double Probability { get; set; }
        public int CashOutStatusId { get; set; }
        public int BetSelectionStatusId { get; set; }
        // Current decimal price at quote time; null when the serving
        // Sportsbook.API predates the field.
        public decimal? Price { get; set; }
    }
}
