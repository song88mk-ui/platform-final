using System.Collections.Generic;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Utility;
using Xunit;

namespace Platform.Shared.Test.Utility
{
    // Covers Helper.GetStake, extracted from duplicated copies in Platform.Betting.API and
    // Platform.Settlement.Service's BettingRepository. Regression guard for the bug where a
    // wholly-void bet's stake tax was deducted the same way as a genuine win, which produced a
    // Bet.Payout less than TrueStake and caused GTS to reject the settlement sync with
    // "Cancelled bets must have a Payout equal to TotalStake".
    public class HelperTests
    {
        private static BetSelection Selection(BetStatus status) => new BetSelection { BetSelectionStatusId = (int)status };

        [Fact]
        public void GetStake_SingleSelectionVoid_ReturnsFullTrueStakeUntaxed()
        {
            var selections = new List<BetSelection> { Selection(BetStatus.Void) };

            Assert.Equal(1.0m, Helper.GetStake(selections, taxedStake: 0.0654m, trueStake: 1.0m));
        }

        [Fact]
        public void GetStake_AllSelectionsCancelled_ReturnsFullTrueStakeUntaxed()
        {
            var selections = new List<BetSelection> { Selection(BetStatus.Cancelled), Selection(BetStatus.Cancelled) };

            Assert.Equal(1.0m, Helper.GetStake(selections, taxedStake: 0.0654m, trueStake: 1.0m));
        }

        [Fact]
        public void GetStake_AllSelectionsVoidOrCancelledMixed_ReturnsFullTrueStakeUntaxed()
        {
            var selections = new List<BetSelection> { Selection(BetStatus.Void), Selection(BetStatus.Cancelled) };

            Assert.Equal(1.0m, Helper.GetStake(selections, taxedStake: 0.0654m, trueStake: 1.0m));
        }

        [Fact]
        public void GetStake_AnySelectionLost_ReturnsFullTrueStake()
        {
            var selections = new List<BetSelection> { Selection(BetStatus.Won), Selection(BetStatus.Lost) };

            Assert.Equal(1.0m, Helper.GetStake(selections, taxedStake: 0.0654m, trueStake: 1.0m));
        }

        [Fact]
        public void GetStake_SingleSelectionWon_ReturnsTrueStakeMinusTaxedStake()
        {
            var selections = new List<BetSelection> { Selection(BetStatus.Won) };

            Assert.Equal(1.0m - 0.0654m, Helper.GetStake(selections, taxedStake: 0.0654m, trueStake: 1.0m));
        }

        [Fact]
        public void GetStake_ParlayWithOneVoidLegAndOneWonLeg_StillTaxed()
        {
            // A partial-void parlay that still resolves to a genuine win keeps the existing
            // taxed behavior - this case is intentionally unchanged; whether it should be
            // untaxed too is a separate, unresolved business/tax-policy question.
            var selections = new List<BetSelection> { Selection(BetStatus.Void), Selection(BetStatus.Won) };

            Assert.Equal(1.0m - 0.0654m, Helper.GetStake(selections, taxedStake: 0.0654m, trueStake: 1.0m));
        }
    }
}
