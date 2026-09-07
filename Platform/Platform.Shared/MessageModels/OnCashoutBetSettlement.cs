using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Providers;
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
        public string ClientCode { get; set; }
        public int ChannelTypeId { get; set; } = (int)ChannelType.BG;
        public CashoutType CashoutType { get; set; }
        public decimal CashOutStake { get; set; }
        public decimal CashOutStakePercent { get; set; }
        public decimal TaxedStake { get; set; }
        public decimal TaxedWinnings { get; set; }
        public decimal NonTaxedPayout { get; set; }
        public bool HasLive { get; set; }
        public string Endpoint { get; set; }
        // Set when re-entering after a GTS "AssessmentDelayed" verdict arrived via Ably
        // (same pattern as OnBetPlacement) — the provider gate then honors the verdict
        // instead of re-assessing.
        public BetAcceptance? DelayedBetAcceptance { get; set; }
        public string DelayedMessage { get; set; }
        public List<CashedOutProbabilities> Probabilities { get; set; }
    }

    public class CashedOutProbabilities
    {
        public Guid BetSelectionId { get; set; }
        // Whether the selection's fixture is live at quote time (per-leg gameState
        // for the GTS cashout assessment; bet-level HasLive is the fallback).
        public bool IsLive { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public double Probability { get; set; }
        public int CashOutStatusId { get; set; }
        // Current decimal price at quote time — the price the confirmed cashout offer
        // was computed from. The GTS cashout assessment must send this, not the
        // placement-time odds: GTS validates submitted prices against its own current
        // prices (price variation limit), so placement odds structurally fail for
        // in-play bets. Null when published by a pre-field Betting.API.
        public decimal? Price { get; set; }
    }
}
