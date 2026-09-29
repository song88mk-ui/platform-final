using Platform.Shared.Calculators;
using Platform.Shared.Enums;

namespace Platform.Shared.Test.Calculators
{
    // GM-868 Phase 1 / GM-866: the cashout value formula previously lived as two independently
    // hand-copied implementations (Betting.API's SportsbookRepository and
    // CashOutStake.Servicev2's BettingRepository) that had drifted apart. This suite pins the
    // now-shared formula's behaviour, including the three named defects GM-866 fixed as part of
    // the extraction. Every expected value below is hand-computed from the formula, not copied
    // from either implementation's prior output.
    public class CashOutCalculatorTests
    {
        private static CashOutSelectionInput ASelection(
            double probability = 0,
            double wonProbability = 0,
            double lostProbability = 0,
            double voidProbability = 0,
            double halfWonProbability = 0,
            double halfLostProbability = 0,
            decimal odds = 2.0m,
            decimal deadHeatFactor = 1.0m,
            bool isBinary = true,
            int betSelectionStatusId = (int)BetStatus.Running,
            decimal? price = null) => new CashOutSelectionInput
            {
                BetSelectionId = Guid.NewGuid(),
                Probability = probability,
                WonProbability = wonProbability,
                LostProbability = lostProbability,
                VoidProbability = voidProbability,
                HalfWonProbability = halfWonProbability,
                HalfLostProbability = halfLostProbability,
                Odds = odds,
                DeadHeatFactor = deadHeatFactor,
                IsBinary = isBinary,
                BetSelectionStatusId = betSelectionStatusId,
                Price = price
            };

        [Fact]
        public void Single_Decimal_StandardBranch_Available()
        {
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 2.0m,
                HasProbabilityChanged = true,
                Selections = new[] { ASelection(probability: 0.5) }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(90m, result.CashOutStake);
            Assert.Equal(0m, result.TaxedStake);
            Assert.Equal(0m, result.TaxedWinnings);
            Assert.Equal(90m, result.NonTaxedPayout);
        }

        [Fact]
        public void Single_Decimal_TaxAndWinningsTax_AppliesCorrectly()
        {
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 110m,
                Stake = 100m,
                StakeTax = 0.10m,
                WinningsTax = 0.20m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 3.0m,
                HasProbabilityChanged = true,
                Selections = new[] { ASelection(probability: 0.5, odds: 3.0m) }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(10m, result.TaxedStake);
            Assert.Equal(130m, result.CashOutStake);
            Assert.Equal(5m, result.TaxedWinnings);
            Assert.Equal(148.5m, result.NonTaxedPayout);
        }

        [Fact]
        public void Single_HongKongOdds_NeverTakesNegativeBranch()
        {
            // HongKong is not one of the two odds types the formula treats as possibly negative
            // (only Indonesian/Malaysian are), regardless of the converted value's sign - so
            // this takes the exact same standard branch as the Decimal test above.
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.HongKong,
                Odds = 2.0m,
                HasProbabilityChanged = true,
                Selections = new[] { ASelection(probability: 0.5, odds: 2.0m, isBinary: true) }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(90m, result.CashOutStake);
        }

        [Fact]
        public void Single_StakeBonus_ReducesTaxableBase_GM866Fix1()
        {
            // GM-866 defect 1: CashOutStake.Servicev2 ignored StakeBonus entirely, so the
            // pushed offer used TrueStake where Betting.API used TrueStake - StakeBonus as the
            // taxedWinnings basis. If StakeBonus stopped being subtracted here, this would
            // compute 129.25 instead of 124.25.
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 110m,
                Stake = 110m,
                StakeBonus = 10m,
                StakeTax = 0m,
                WinningsTax = 0.5m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 3.0m,
                HasProbabilityChanged = true,
                Selections = new[] { ASelection(probability: 0.5, odds: 3.0m) }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(124.25m, result.CashOutStake);
        }

        [Fact]
        public void Single_NegativeMalaysianOdds_NonTaxedPayoutUsesAddition_GM866Fix2()
        {
            // GM-866 defect 2: SportsbookRepository.cs:361 computed
            // nonTaxedPayout = (TrueStake * Stake) * probability in this branch, where every
            // other branch of the same method (e.g. the probability-breakdown branch just
            // above it) uses TrueStake + Stake. Left as the bug, this test's NonTaxedPayout
            // would be 3600 instead of 72.
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.Malaysian,
                Odds = 3.0m,
                HasProbabilityChanged = true,
                // Odds > 2.00 with IsBinary=true converts to a negative Malaysian price.
                Selections = new[] { ASelection(probability: 0.4, odds: 3.0m, isBinary: true) }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(72m, result.CashOutStake);
            Assert.Equal(72m, result.NonTaxedPayout);
        }

        [Fact]
        public void Single_NegativeIndonesianOdds_UsesNegativeBranchFormula()
        {
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.Indonesian,
                Odds = 1.5m,
                HasProbabilityChanged = true,
                // Odds between 1 and 2 with IsBinary=true converts to a negative Indonesian price.
                Selections = new[] { ASelection(probability: 0.4, odds: 1.5m, isBinary: true) }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(72m, result.CashOutStake);
        }

        [Fact]
        public void Single_ProbabilityBreakdown_ComputesWeightedPayout()
        {
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 2.0m,
                HasProbabilityChanged = true,
                Selections = new[]
                {
                    ASelection(odds: 2.0m, wonProbability: 0.6, lostProbability: 0.2, voidProbability: 0.1, halfWonProbability: 0.05, halfLostProbability: 0.05)
                }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(126m, result.CashOutStake);
            Assert.Equal(126m, result.NonTaxedPayout);
        }

        [Fact]
        public void Single_PriceIsIncludedInOutput_GM866Fix3()
        {
            // GM-866 defect 3: singles used to re-project the output Probabilities without
            // Price (parlays passed the original objects through and kept it), so GTS always
            // fell back to placement odds for singles.
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 2.0m,
                HasProbabilityChanged = true,
                Selections = new[] { ASelection(probability: 0.5, price: 5.25m) }
            });

            var probability = Assert.Single(result.Probabilities);
            Assert.Equal(5.25m, probability.Price);
        }

        [Fact]
        public void Single_ExceedsCeiling_ReturnsUnavailable()
        {
            // WonProbability above 1 is not a value real probability data should carry, but the
            // formula has no floor/ceiling on the inputs themselves - only on the result - so
            // this is the cleanest way to force the ceiling guard's Unavailable path.
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 2.0m,
                HasProbabilityChanged = true,
                Selections = new[] { ASelection(odds: 2.0m, wonProbability: 1.5) }
            });

            Assert.Equal(CashOutStatus.Unavailable, result.CashOutStatus);
            Assert.Equal(0m, result.CashOutStake);
        }

        [Fact]
        public void Single_ProbabilityUnchanged_RefundsStakeMinusMargin()
        {
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 110m,
                Stake = 100m,
                StakeTax = 0.10m,
                WinningsTax = 0.5m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 2.0m,
                HasProbabilityChanged = false,
                Selections = new[] { ASelection(probability: 0.5, odds: 2.0m) }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(90m, result.CashOutStake);
            Assert.Equal(99m, result.NonTaxedPayout);
        }

        [Fact]
        public void Parlay_AllRunning_ComputesCombinedOdds()
        {
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 0m, // unused by the parlay branch
                HasProbabilityChanged = true,
                Selections = new[]
                {
                    ASelection(probability: 0.8, odds: 2.0m, betSelectionStatusId: (int)BetStatus.Running),
                    ASelection(probability: 0.9, odds: 1.5m, betSelectionStatusId: (int)BetStatus.Running)
                }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(194.4m, result.CashOutStake);
        }

        [Fact]
        public void Parlay_SettledLegForcedToProbabilityOne_UsesDeadHeatFactorInTotalOdds()
        {
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 0m,
                HasProbabilityChanged = true,
                Selections = new[]
                {
                    // Won leg: its own Probability (0.3, a decoy) must be ignored and forced to
                    // 1, and its contribution to totalOdds is Odds * DeadHeatFactor.
                    ASelection(probability: 0.3, odds: 2.0m, deadHeatFactor: 0.5m, betSelectionStatusId: (int)BetStatus.Won),
                    ASelection(probability: 0.8, odds: 1.5m, betSelectionStatusId: (int)BetStatus.Running)
                }
            });

            Assert.Equal(CashOutStatus.Available, result.CashOutStatus);
            Assert.Equal(108m, result.CashOutStake);
            Assert.Equal(1, result.Probabilities.First(p => p.BetSelectionStatusId == (int)BetStatus.Won).Probability);
        }

        [Fact]
        public void Parlay_ExceedsCeiling_ReturnsUnavailable()
        {
            var result = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = 100m,
                Stake = 100m,
                StakeTax = 0m,
                WinningsTax = 0m,
                OddsTypeId = (int)OddsType.Decimal,
                Odds = 0m,
                HasProbabilityChanged = true,
                Selections = new[]
                {
                    ASelection(probability: 2.0, odds: 2.0m, betSelectionStatusId: (int)BetStatus.Running),
                    ASelection(probability: 2.0, odds: 2.0m, betSelectionStatusId: (int)BetStatus.Running)
                }
            });

            Assert.Equal(CashOutStatus.Unavailable, result.CashOutStatus);
            Assert.Equal(0m, result.CashOutStake);
        }
    }
}
