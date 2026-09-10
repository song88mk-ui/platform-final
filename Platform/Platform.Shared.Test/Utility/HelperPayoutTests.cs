using System;
using System.Collections.Generic;
using Platform.Shared.Enums;
using Platform.Shared.Utility;
using Xunit;

namespace Platform.Shared.Test.Utility
{
    // Covers Helper.GetPayout, which computes what a settled bet pays out. It had no tests at all
    // before GM-804, despite being the money calculation at the end of every settlement.
    //
    // The method has three independent branches, and they disagree with each other on purpose:
    //
    //  1. Single selection whose converted odds are negative, and only for Indonesian or
    //     Malaysian odds types. A flat switch on BetStatus. HalfWon here pays
    //     (stake / 2) + trueStake.
    //  2. isBetBuilder. Checked in priority order: all-won-or-running, then any Lost, then any
    //     Void, then any Cancelled, then a catch-all - and the last three all return trueStake.
    //  3. Everything else: a per-selection factor accumulated by multiplication, then scaled by
    //     trueStake. HalfWon here contributes (odds + 1) / 2, NOT the branch-1 formula.
    //
    // The HalfWon asymmetry between branch 1 and branch 3 is the single most dangerous thing in
    // this method, so it is asserted from both sides.
    //
    // Tuple layout is (BetStatus, odds, factor, isBinary): Item2 is the odds, Item3 the
    // per-selection factor multiplied into the running total for Won and Running, and Item4 the
    // isBinary flag passed through to OddsTypeHelper.
    public class HelperPayoutTests
    {
        private static Tuple<BetStatus, decimal, decimal, bool> Sel(
            BetStatus status, decimal odds = 2.0m, decimal factor = 1.0m, bool isBinary = false) =>
            Tuple.Create(status, odds, factor, isBinary);

        private static List<Tuple<BetStatus, decimal, decimal, bool>> One(
            BetStatus status, decimal odds = 2.0m, decimal factor = 1.0m, bool isBinary = false) =>
            new List<Tuple<BetStatus, decimal, decimal, bool>> { Sel(status, odds, factor, isBinary) };

        // Branch 1 requires OddsTypeHelper.GetOddsByOddsType to return a negative number. For
        // Malaysian that means odds above 2.00 with isBinary true: 3.0 converts to -0.5.
        private const decimal NegativeConvertingOdds = 3.0m;

        #region Branch 1 - single selection, negative converted odds, Indonesian or Malaysian

        [Theory]
        [InlineData(OddsType.Malaysian)]
        [InlineData(OddsType.Indonesian)]
        public void GetPayout_SingleNegativeOddsLost_ReturnsZero(OddsType oddsType)
        {
            var selections = One(BetStatus.Lost, NegativeConvertingOdds, isBinary: true);

            Assert.Equal(0m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, oddsType));
        }

        [Fact]
        public void GetPayout_SingleNegativeOddsHalfWon_ReturnsHalfStakePlusTrueStake()
        {
            // (stake / 2) + trueStake = 45 + 100. Note this uses stake, not odds.
            var selections = One(BetStatus.HalfWon, NegativeConvertingOdds, isBinary: true);

            Assert.Equal(145m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.Malaysian));
        }

        [Fact]
        public void GetPayout_SingleNegativeOddsHalfLost_ReturnsHalfTrueStake()
        {
            var selections = One(BetStatus.HalfLost, NegativeConvertingOdds, isBinary: true);

            Assert.Equal(50m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.Malaysian));
        }

        [Theory]
        [InlineData(BetStatus.Won)]
        [InlineData(BetStatus.Running)]
        public void GetPayout_SingleNegativeOddsWonOrRunning_ReturnsTrueStakePlusStake(BetStatus status)
        {
            var selections = One(status, NegativeConvertingOdds, isBinary: true);

            Assert.Equal(190m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.Malaysian));
        }

        [Theory]
        [InlineData(BetStatus.Void)]
        [InlineData(BetStatus.Cancelled)]
        public void GetPayout_SingleNegativeOddsVoidOrCancelled_ReturnsTrueStake(BetStatus status)
        {
            var selections = One(status, NegativeConvertingOdds, isBinary: true);

            Assert.Equal(100m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.Malaysian));
        }

        [Theory]
        [InlineData(OddsType.Decimal)]
        [InlineData(OddsType.HongKong)]
        public void GetPayout_NegativeOddsButNotIndonesianOrMalaysian_FallsThroughToTheLoop(OddsType oddsType)
        {
            // The odds-type guard is an explicit part of branch 1. Hong Kong never converts
            // negative anyway, but the guard must hold even so: this falls through to branch 3,
            // where Won contributes odds * factor.
            var selections = One(BetStatus.Won, odds: 3.0m, factor: 1.0m, isBinary: true);

            Assert.Equal(300m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, oddsType));
        }

        [Fact]
        public void GetPayout_SinglePositiveConvertedOdds_FallsThroughToTheLoop()
        {
            // 1.5 Malaysian converts to +0.5, so branch 1 does not apply despite the odds type.
            var selections = One(BetStatus.Won, odds: 1.5m, factor: 2.0m, isBinary: true);

            Assert.Equal(300m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.Malaysian));
        }

        [Fact]
        public void GetPayout_TwoSelectionsWithNegativeOdds_FallsThroughToTheLoop()
        {
            // Branch 1 is single-selection only.
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>>
            {
                Sel(BetStatus.Void, NegativeConvertingOdds, isBinary: true),
                Sel(BetStatus.Void, NegativeConvertingOdds, isBinary: true)
            };

            Assert.Equal(100m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.Malaysian));
        }

        #endregion

        #region Branch 2 - bet builder

        [Fact]
        public void GetPayout_BetBuilderAllWon_ReturnsOddsTimesTrueStake()
        {
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>> { Sel(BetStatus.Won), Sel(BetStatus.Won) };

            Assert.Equal(250m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: true, odds: 2.5m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_BetBuilderWonAndRunningMixed_ReturnsOddsTimesTrueStake()
        {
            // Running counts toward the all-won branch, so an unsettled leg still projects a win.
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>> { Sel(BetStatus.Won), Sel(BetStatus.Running) };

            Assert.Equal(250m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: true, odds: 2.5m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_BetBuilderAnyLost_ReturnsZero()
        {
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>> { Sel(BetStatus.Won), Sel(BetStatus.Lost) };

            Assert.Equal(0m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: true, odds: 2.5m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_BetBuilderLostBeatsVoid_ReturnsZero()
        {
            // Priority order matters: Lost is checked before Void, so a bet with both pays zero,
            // not the stake back.
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>> { Sel(BetStatus.Lost), Sel(BetStatus.Void) };

            Assert.Equal(0m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: true, odds: 2.5m, OddsType.Decimal));
        }

        [Theory]
        [InlineData(BetStatus.Void)]
        [InlineData(BetStatus.Cancelled)]
        [InlineData(BetStatus.HalfWon)]
        public void GetPayout_BetBuilderNoWinNoLoss_ReturnsTrueStake(BetStatus status)
        {
            // Void, Cancelled and the catch-all all return trueStake. HalfWon reaches the
            // catch-all, so a half-won bet builder returns the stake rather than a half payout.
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>> { Sel(BetStatus.Won), Sel(status) };

            Assert.Equal(100m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: true, odds: 2.5m, OddsType.Decimal));
        }

        #endregion

        #region Branch 3 - the accumulator loop

        [Fact]
        public void GetPayout_LoopWon_MultipliesOddsByFactor()
        {
            var selections = One(BetStatus.Won, odds: 2.0m, factor: 1.5m);

            Assert.Equal(300m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_LoopRunning_MultipliesOddsByFactorLikeWon()
        {
            var selections = One(BetStatus.Running, odds: 2.0m, factor: 1.5m);

            Assert.Equal(300m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_LoopLost_ContributesZeroAndZeroesTheWholeBet()
        {
            // One lost leg multiplies the accumulator by zero, so the whole parlay pays nothing.
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>>
            {
                Sel(BetStatus.Won, odds: 2.0m, factor: 1.0m),
                Sel(BetStatus.Lost)
            };

            Assert.Equal(0m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_LoopHalfWon_ContributesOddsPlusOneOverTwo()
        {
            // (odds + 1) / 2 = (3 + 1) / 2 = 2. This is the formula branch 1 does NOT use.
            var selections = One(BetStatus.HalfWon, odds: 3.0m);

            Assert.Equal(200m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_LoopHalfLost_ContributesOneHalf()
        {
            var selections = One(BetStatus.HalfLost, odds: 3.0m);

            Assert.Equal(50m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        [Theory]
        [InlineData(BetStatus.Void)]
        [InlineData(BetStatus.Cancelled)]
        public void GetPayout_LoopVoidOrCancelled_ContributeOneAndAreNeutral(BetStatus status)
        {
            // A void leg must not change the payout of the legs around it.
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>>
            {
                Sel(BetStatus.Won, odds: 2.0m, factor: 1.0m),
                Sel(status)
            };

            Assert.Equal(200m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_LoopMultipliesAcrossLegs()
        {
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>>
            {
                Sel(BetStatus.Won, odds: 2.0m, factor: 1.0m),
                Sel(BetStatus.Won, odds: 3.0m, factor: 1.0m)
            };

            Assert.Equal(600m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_LoopScalesByTrueStake()
        {
            var selections = One(BetStatus.Won, odds: 2.0m, factor: 1.0m);

            Assert.Equal(50m, Helper.GetPayout(selections, trueStake: 25m, stake: 20m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_LoopHalfWonAndHalfLostCombined()
        {
            // (3 + 1) / 2 * 0.5 = 1, so the two half outcomes cancel to the stake.
            var selections = new List<Tuple<BetStatus, decimal, decimal, bool>>
            {
                Sel(BetStatus.HalfWon, odds: 3.0m),
                Sel(BetStatus.HalfLost)
            };

            Assert.Equal(100m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        #endregion

        [Fact]
        public void GetPayout_SingleNegativeOddsLostWithZeroTrueStake_ReturnsZero()
        {
            // The Lost arm is written 0m * trueStake. Guards the multiplication specifically:
            // a division would throw here rather than return zero.
            var selections = One(BetStatus.Lost, NegativeConvertingOdds, isBinary: true);

            Assert.Equal(0m, Helper.GetPayout(selections, trueStake: 0m, stake: 0m, isBetBuilder: false, odds: 3m, OddsType.Malaysian));
        }

        [Fact]
        public void GetPayout_ConvertedOddsExactlyZero_FallsThroughToTheLoop()
        {
            // Malaysian odds of 1.00 convert to exactly 0, and the guard is strictly less than
            // zero, so this must NOT take branch 1. Branch 1 would pay trueStake + stake = 190.
            var selections = One(BetStatus.Won, odds: 1.0m, factor: 1.0m, isBinary: true);

            Assert.Equal(100m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.Malaysian));
        }

        [Fact]
        public void GetPayout_HongKongWithNegativeConvertedOdds_StillFallsThroughToTheLoop()
        {
            // Hong Kong odds below 1.00 do convert negative, so the odds-type guard is the only
            // thing keeping this out of branch 1. Branch 1 would pay 190 instead of 50.
            var selections = One(BetStatus.Won, odds: 0.5m, factor: 1.0m, isBinary: true);

            Assert.Equal(50m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.HongKong));
        }

        [Theory]
        [InlineData(BetStatus.Void)]
        [InlineData(BetStatus.Cancelled)]
        public void GetPayout_LoopSingleVoidOrCancelled_ReturnsTrueStake(BetStatus status)
        {
            // A lone void leg must still contribute its factor of 1. If the Add were skipped the
            // accumulator would be empty and Aggregate would throw.
            var selections = One(status);

            Assert.Equal(100m, Helper.GetPayout(selections, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 0m, OddsType.Decimal));
        }

        [Fact]
        public void GetPayout_HalfWonFormulaDiffersBetweenSingleNegativeOddsAndTheLoop()
        {
            // The asymmetry, asserted directly. Same status, same odds, same stakes: branch 1 pays
            // (stake / 2) + trueStake while branch 3 pays (odds + 1) / 2 * trueStake. Anyone
            // unifying these two formulas must do it deliberately.
            var single = One(BetStatus.HalfWon, NegativeConvertingOdds, isBinary: true);
            var viaLoop = One(BetStatus.HalfWon, NegativeConvertingOdds, isBinary: false);

            var branchOne = Helper.GetPayout(single, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.Malaysian);
            var branchThree = Helper.GetPayout(viaLoop, trueStake: 100m, stake: 90m, isBetBuilder: false, odds: 3m, OddsType.Malaysian);

            Assert.Equal(145m, branchOne);
            Assert.Equal(200m, branchThree);
            Assert.NotEqual(branchOne, branchThree);
        }
    }
}
