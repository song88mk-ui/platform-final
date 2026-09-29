using Platform.Shared.Enums;
using Platform.Shared.Helpers;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Shared.Calculators
{
    // The cashout value formula shared by Betting.API (the live quote,
    // Platform.Betting.API/Repository/SportsbookRepository.cs GetCashOut) and
    // CashOutStake.Servicev2 (the pushed offer,
    // Platform.CashOutStake.Servicev2/Repository/BettingRepository.cs GetCashOut) -
    // previously two independently hand-copied implementations that had drifted (GM-866):
    // the pushed offer ignored StakeBonus, a negative-odds branch used the wrong operator,
    // and singles dropped the quote-time Price that parlays kept. This is the Betting.API
    // implementation (the reference side per GM-866) migrated here with those three fixes,
    // so both callers now compute the identical value for the identical bet.
    //
    // Deliberately scoped to the value formula only, not eligibility/Unavailable-reason
    // checking (suspended markets, settled selections, global betstop, etc.) - the two
    // callers determine eligibility from different, non-interchangeable state (a live
    // Sportsbook.API probability response vs a Mongo-backed local cache), so each caller
    // still decides eligibility on its own and calls this only once a bet's selections are
    // already known-eligible.
    public static class CashOutCalculator
    {
        private const decimal MarginRate = 0.10m;

        public static CashOutCalculation Calculate(CashOutCalculationInput input)
        {
            var finalStake = input.TrueStake - input.StakeBonus;
            var taxedStake = finalStake - (finalStake / (1 + input.StakeTax));
            var stake = input.TrueStake - taxedStake;
            var nonTaxedStake = input.TrueStake;

            return input.Selections.Count == 1
                ? CalculateSingle(input, input.Selections[0], finalStake, taxedStake, stake, nonTaxedStake)
                : CalculateParlay(input, finalStake, taxedStake, stake, nonTaxedStake);
        }

        private static CashOutCalculation CalculateSingle(CashOutCalculationInput input, CashOutSelectionInput selection, decimal finalStake, decimal taxedStake, decimal stake, decimal nonTaxedStake)
        {
            var isNegativeIndoOrMalay = OddsTypeHelper.GetOddsByOddsType(input.OddsTypeId, selection.Odds, selection.IsBinary) < 0
                && (input.OddsTypeId == (int)OddsType.Indonesian || input.OddsTypeId == (int)OddsType.Malaysian);

            decimal cashOutStake;
            var nonTaxedPayout = 0m;

            if (selection.WonProbability > 0 || selection.LostProbability > 0 || selection.VoidProbability > 0 || selection.HalfWonProbability > 0 || selection.HalfLostProbability > 0)
            {
                decimal won, halfWon, nonTaxedWon, nonTaxedHalfWon;

                if (isNegativeIndoOrMalay)
                {
                    won = Convert.ToDecimal(selection.WonProbability) * (stake + input.Stake);
                    halfWon = Convert.ToDecimal(selection.HalfWonProbability) * ((input.Stake / 2) + stake);
                    nonTaxedWon = Convert.ToDecimal(selection.WonProbability) * (input.TrueStake + input.Stake);
                    nonTaxedHalfWon = Convert.ToDecimal(selection.HalfWonProbability) * ((input.Stake / 2) + stake);
                }
                else
                {
                    won = Convert.ToDecimal(selection.WonProbability) * (stake * input.Odds);
                    halfWon = Convert.ToDecimal(selection.HalfWonProbability) * (stake * ((input.Odds + 1) / 2));
                    nonTaxedWon = Convert.ToDecimal(selection.WonProbability) * (nonTaxedStake * input.Odds);
                    nonTaxedHalfWon = Convert.ToDecimal(selection.HalfWonProbability) * (nonTaxedStake * ((input.Odds + 1) / 2));
                }

                var lost = Convert.ToDecimal(selection.LostProbability) * (stake * 0);
                var voidd = Convert.ToDecimal(selection.VoidProbability) * (stake * 1);
                var halfLost = Convert.ToDecimal(selection.HalfLostProbability) * (stake / 2);

                cashOutStake = won + lost + voidd + halfWon + halfLost;

                var nonTaxedLost = Convert.ToDecimal(selection.LostProbability) * (nonTaxedStake * 0);
                var nonTaxedVoidd = Convert.ToDecimal(selection.VoidProbability) * (nonTaxedStake * 1);
                var nonTaxedHalfLost = Convert.ToDecimal(selection.HalfLostProbability) * (nonTaxedStake / 2);

                nonTaxedPayout = nonTaxedWon + nonTaxedLost + nonTaxedVoidd + nonTaxedHalfWon + nonTaxedHalfLost;
            }
            else
            {
                if (isNegativeIndoOrMalay)
                {
                    cashOutStake = (stake + input.Stake) * Convert.ToDecimal(selection.Probability);
                    // GM-866: the pre-extraction Betting.API code multiplied TrueStake * Stake
                    // here (SportsbookRepository.cs:361) where every other branch of this same
                    // method uses TrueStake + Stake (e.g. nonTaxedWon above) - fixed.
                    nonTaxedPayout = (input.TrueStake + input.Stake) * Convert.ToDecimal(selection.Probability);
                }
                else
                {
                    cashOutStake = (stake * input.Odds) * Convert.ToDecimal(selection.Probability);
                    nonTaxedPayout = (nonTaxedStake * input.Odds) * Convert.ToDecimal(selection.Probability);
                }
            }

            cashOutStake -= cashOutStake * MarginRate;
            nonTaxedPayout -= nonTaxedPayout * MarginRate;

            var taxedWinnings = (cashOutStake - finalStake) * input.WinningsTax;

            if (cashOutStake > stake)
                cashOutStake -= taxedWinnings;
            else
                taxedWinnings = 0;

            var finalCashOutStake = cashOutStake;

            if (!input.HasProbabilityChanged)
            {
                finalCashOutStake = stake - (stake * MarginRate);
                nonTaxedPayout = nonTaxedStake - (nonTaxedStake * MarginRate);
            }

            var ceiling = isNegativeIndoOrMalay ? stake + input.Stake : stake * input.Odds;
            var isAvailable = finalCashOutStake <= ceiling;

            return BuildResult(isAvailable, finalCashOutStake, taxedStake, taxedWinnings, nonTaxedPayout, input.HasLive, input.Selections);
        }

        private static CashOutCalculation CalculateParlay(CashOutCalculationInput input, decimal finalStake, decimal taxedStake, decimal stake, decimal nonTaxedStake)
        {
            var settledStatuses = new HashSet<int> { (int)BetStatus.Won, (int)BetStatus.HalfWon, (int)BetStatus.HalfLost, (int)BetStatus.Void };

            // A settled leg's own probability no longer reflects "will this leg win" (it
            // already has), so it is forced to 1 before the parlay's combined probability is
            // multiplied out - matching each original implementation exactly.
            var parlaySelections = input.Selections
                .Select(s => settledStatuses.Contains(s.BetSelectionStatusId) ? s with { Probability = 1 } : s)
                .ToList();

            var finalProbability = parlaySelections.Where(c => c.Probability > 0).Select(s => s.Probability).Aggregate((a, b) => a * b);
            var totalOdds = GetTotalOdds(input.Selections);

            var cashOutStake = (stake * totalOdds) * Convert.ToDecimal(finalProbability);
            cashOutStake -= cashOutStake * MarginRate;

            var nonTaxedPayout = (nonTaxedStake * totalOdds) * Convert.ToDecimal(finalProbability);
            nonTaxedPayout -= nonTaxedPayout * MarginRate;

            var taxedWinnings = (cashOutStake - finalStake) * input.WinningsTax;

            if (cashOutStake > stake)
                cashOutStake -= taxedWinnings;
            else
                taxedWinnings = 0;

            var finalCashOutStake = cashOutStake;

            if (!input.HasProbabilityChanged)
            {
                finalCashOutStake = stake - (stake * MarginRate);
                nonTaxedPayout = nonTaxedStake - (nonTaxedStake * MarginRate);
            }

            var isAvailable = finalCashOutStake != 0 && finalCashOutStake <= stake * totalOdds;

            return BuildResult(isAvailable, finalCashOutStake, taxedStake, taxedWinnings, nonTaxedPayout, input.HasLive, parlaySelections);
        }

        private static decimal GetTotalOdds(IReadOnlyList<CashOutSelectionInput> selections)
        {
            var values = new List<decimal>();

            foreach (var selection in selections)
            {
                switch ((BetStatus)selection.BetSelectionStatusId)
                {
                    case BetStatus.Lost:
                        values.Add(0m);
                        break;
                    case BetStatus.HalfWon:
                        values.Add((selection.Odds + 1) / 2);
                        break;
                    case BetStatus.HalfLost:
                        values.Add(0.5m);
                        break;
                    case BetStatus.Won:
                        values.Add(selection.Odds * selection.DeadHeatFactor);
                        break;
                    case BetStatus.Void:
                        values.Add(1m);
                        break;
                    case BetStatus.Cancelled:
                        values.Add(1m);
                        break;
                    case BetStatus.Running:
                        values.Add(selection.Odds);
                        break;
                }
            }

            return values.Aggregate((a, b) => a * b);
        }

        private static CashOutCalculation BuildResult(bool isAvailable, decimal finalCashOutStake, decimal taxedStake, decimal taxedWinnings, decimal nonTaxedPayout, bool hasLive, IReadOnlyList<CashOutSelectionInput> selections)
        {
            if (!isAvailable)
            {
                return new CashOutCalculation
                {
                    CashOutStatus = CashOutStatus.Unavailable,
                    CashOutStake = 0m
                };
            }

            return new CashOutCalculation
            {
                CashOutStatus = CashOutStatus.Available,
                CashOutStake = finalCashOutStake,
                TaxedStake = taxedStake,
                TaxedWinnings = taxedWinnings,
                NonTaxedPayout = nonTaxedPayout,
                HasLive = hasLive,
                Probabilities = selections.Select(s => new Probabilities
                {
                    BetSelectionId = s.BetSelectionId,
                    IsLive = s.IsLive,
                    CashOutStatusId = 1,
                    Price = s.Price,
                    Probability = s.Probability,
                    WonProbability = s.WonProbability,
                    LostProbability = s.LostProbability,
                    VoidProbability = s.VoidProbability,
                    HalfWonProbability = s.HalfWonProbability,
                    HalfLostProbability = s.HalfLostProbability,
                    BetSelectionStatusId = s.BetSelectionStatusId
                }).ToList()
            };
        }
    }

    public class CashOutCalculationInput
    {
        public decimal TrueStake { get; init; }
        public decimal Stake { get; init; }
        public decimal StakeBonus { get; init; }
        public decimal StakeTax { get; init; }
        public decimal WinningsTax { get; init; }
        public int OddsTypeId { get; init; }
        // Bet-level odds, used by the singles payout formula. For a single-selection bet
        // this is expected to equal Selections[0].Odds, but the original implementations
        // keep them as separate fields (bet.Odds vs selections.First().Odds), so this does
        // too rather than assuming they can never diverge.
        public decimal Odds { get; init; }
        public bool HasProbabilityChanged { get; init; }
        public bool HasLive { get; init; }
        public IReadOnlyList<CashOutSelectionInput> Selections { get; init; } = Array.Empty<CashOutSelectionInput>();
    }

    public record CashOutSelectionInput
    {
        public Guid BetSelectionId { get; init; }
        public decimal Odds { get; init; }
        public decimal DeadHeatFactor { get; init; }
        public bool IsBinary { get; init; }
        public int BetSelectionStatusId { get; init; }
        public double Probability { get; init; }
        public double WonProbability { get; init; }
        public double LostProbability { get; init; }
        public double VoidProbability { get; init; }
        public double HalfWonProbability { get; init; }
        public double HalfLostProbability { get; init; }
        public decimal? Price { get; init; }
        public bool IsLive { get; init; }
    }

    public class CashOutCalculation
    {
        public CashOutStatus CashOutStatus { get; init; }
        public decimal CashOutStake { get; init; }
        public decimal TaxedStake { get; init; }
        public decimal TaxedWinnings { get; init; }
        public decimal NonTaxedPayout { get; init; }
        public bool HasLive { get; init; }
        public List<Probabilities> Probabilities { get; init; } = new();
    }
}
