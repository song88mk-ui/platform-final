using Platform.Operator.API.Utility;
using Platform.Shared.Enums;

namespace Platform.Operator.API.Test.Utility
{
    // Covers OperatorAuditReversal.Resolve over the FULL (previous, incoming) BetStatus matrix.
    // The four rule theories partition the 11 x 10 pairs (the 10 statuses plus "no previous row",
    // against the 10 incoming statuses) and EveryStatusPairIsClaimedByExactlyOneRule proves the
    // partition is complete, so a new BetStatus member fails here until a rule claims it.
    //
    // Numbers are the GM-815 reproduction values: stake 30, a Won payout of 90, a refund of 30,
    // and a previous row of 50 so that a reversal shows up as its own term.
    public class OperatorAuditReversalTests
    {
        private const decimal Stake = 30m;
        private const decimal Payout = 90m;
        private const decimal Refund = 30m;
        private const decimal PreviousAmount = 50m;

        private static readonly BetStatus[] All = Enum.GetValues<BetStatus>();

        // Rows that a later settlement reverses.
        private static readonly BetStatus[] SettledNonVoid = { BetStatus.Won, BetStatus.HalfWon, BetStatus.Lost, BetStatus.HalfLost, BetStatus.CashedOut };

        // Rows that are never reversed. Running is the placement debit (or a partial-cashout
        // credit). Cancelled is a refund like Void but has never been in the reversible set, and
        // GM-815 leaves that as it was: a settlement after a Cancelled row still pays on top of it.
        private static readonly BetStatus[] NeverReversed = { BetStatus.Waiting, BetStatus.Running, BetStatus.Rejected, BetStatus.Cancelled };

        public static IEnumerable<object[]> NoReversalPairs =>
            Pairs(NeverReversed.Cast<BetStatus?>().Prepend(null), All);

        public static IEnumerable<object[]> PreviousVoidPairs =>
            Pairs(new BetStatus?[] { BetStatus.Void }, All);

        public static IEnumerable<object[]> IncomingVoidPairs =>
            Pairs(SettledNonVoid.Cast<BetStatus?>(), new[] { BetStatus.Void });

        public static IEnumerable<object[]> SettledToSettledPairs =>
            Pairs(SettledNonVoid.Cast<BetStatus?>(), All.Where(s => s != BetStatus.Void));

        private static IEnumerable<object[]> Pairs(IEnumerable<BetStatus?> previous, IEnumerable<BetStatus> incoming) =>
            previous.SelectMany(p => incoming.Select(i => new object[] { p, i }));

        [Fact]
        public void EveryStatusPairIsClaimedByExactlyOneRule()
        {
            var claimed = NoReversalPairs.Concat(PreviousVoidPairs).Concat(IncomingVoidPairs).Concat(SettledToSettledPairs)
                .Select(pair => ((BetStatus?)pair[0], (BetStatus)pair[1]))
                .ToList();

            var expected = All.Cast<BetStatus?>().Prepend(null)
                .SelectMany(p => All.Select(i => (p, i)))
                .ToList();

            Assert.Equal(expected.Count, claimed.Count);
            Assert.Equal(expected.Count, claimed.Distinct().Count());
            Assert.Empty(expected.Except(claimed));
        }

        #region Rule 1: nothing to reverse

        [Theory]
        [MemberData(nameof(NoReversalPairs))]
        public void NoPreviousRowOrNotASettledOutcome_BooksTheIncomingAmountAndReversesNothing(BetStatus? previous, BetStatus incoming)
        {
            // Running is the one status that debits: the placement takes the stake. Every other
            // status credits its payout. hasVoid cannot matter when there is nothing to reverse.
            var expectedAmount = incoming == BetStatus.Running ? -Stake : Payout;

            foreach (var hasVoid in new[] { false, true })
            {
                var movement = OperatorAuditReversal.Resolve(incoming, Payout, Stake, false, false, previous, PreviousAmount, hasVoid);

                Assert.Equal((expectedAmount, 0m), movement);
            }
        }

        [Fact]
        public void RunningFreeBet_DebitsNothing()
        {
            Assert.Equal((0m, 0m), OperatorAuditReversal.Resolve(BetStatus.Running, 0m, Stake, isFreebet: true, isPartialCashout: false, null, 0m, false));
        }

        [Fact]
        public void RunningPartialCashout_CreditsThePayoutInsteadOfDebitingTheStake()
        {
            // Without the flag a partial cashout of a still-running bet would debit the stake again.
            Assert.Equal((15m, 0m), OperatorAuditReversal.Resolve(BetStatus.Running, 15m, Stake, isFreebet: false, isPartialCashout: true, null, 0m, false));
        }

        [Fact]
        public void PreviousTransactionTypeOutsideTheEnum_IsNotReversed()
        {
            // TransactionTypeId is a plain int column; an unknown value falls through like the
            // switch default it replaces.
            Assert.Equal((Payout, 0m), OperatorAuditReversal.Resolve(BetStatus.Won, Payout, Stake, false, false, (BetStatus)9999, PreviousAmount, false));
        }

        #endregion

        #region Rule 2: the previous row is a Void

        [Theory]
        [MemberData(nameof(PreviousVoidPairs))]
        public void PreviousRowIsAVoid_BooksPayoutMinusStakeAndLeavesTheRefundStanding(BetStatus? previous, BetStatus incoming)
        {
            // The Void refunded the stake, so the balance is back where it was before the placement
            // and the new outcome is booked against that baseline. payout - stake is "reclaim the
            // refund, then credit the payout" collapsed into one amount: with the placement debit in
            // the history the balance ends at placement - stake + payout, exactly as if the bet had
            // been settled that way first time. hasVoid is necessarily true here.
            var movement = OperatorAuditReversal.Resolve(incoming, Payout, Stake, false, false, previous, Refund, hasVoid: true);

            Assert.Equal((Payout - Stake, 0m), movement);
        }

        [Fact]
        public void VoidThenVoidWithTheSameRefund_BooksNothing()
        {
            // Before GM-815 a repeated Void was skipped by the "status changed" guard and refunded
            // the stake a second time.
            Assert.Equal((0m, 0m), OperatorAuditReversal.Resolve(BetStatus.Void, Refund, Stake, false, false, BetStatus.Void, Refund, hasVoid: true));
        }

        [Fact]
        public void VoidThenLost_TakesTheStakeBack()
        {
            Assert.Equal((-Stake, 0m), OperatorAuditReversal.Resolve(BetStatus.Lost, 0m, Stake, false, false, BetStatus.Void, Refund, hasVoid: true));
        }

        #endregion

        #region Rule 3: the incoming status is a Void

        [Theory]
        [MemberData(nameof(IncomingVoidPairs))]
        public void IncomingVoidAfterASettledOutcome_ReversesThePreviousAmountInsideTheAmount(BetStatus? previous, BetStatus incoming)
        {
            // Paid 50, now Void: take the 50 back and refund the 30 stake, as one -20 movement.
            Assert.Equal((Refund - PreviousAmount, 0m), OperatorAuditReversal.Resolve(incoming, Refund, Stake, false, false, previous, PreviousAmount, hasVoid: false));

            // With an earlier Void in the history the previous row was already net of the stake, so
            // the refund is too: (30 - 30) - 50.
            Assert.Equal((Refund - Stake - PreviousAmount, 0m), OperatorAuditReversal.Resolve(incoming, Refund, Stake, false, false, previous, PreviousAmount, hasVoid: true));
        }

        #endregion

        #region Rule 4: settled outcome followed by anything but a Void

        [Theory]
        [MemberData(nameof(SettledToSettledPairs))]
        public void SettledOutcomeFollowedByAnythingButVoid_ReversesThePreviousAmountThroughThePreBalance(BetStatus? previous, BetStatus incoming)
        {
            // Includes the SAME status arriving twice (Won then Won): before GM-815 that pair was
            // skipped and the second payout stacked on the first. A settled bet does not
            // realistically go back to Waiting, Running, Rejected or Cancelled; those pairs are
            // pinned to the same rule as today, not endorsed.
            var expectedAmount = incoming == BetStatus.Running ? -Stake : Payout;

            Assert.Equal((expectedAmount, -PreviousAmount), OperatorAuditReversal.Resolve(incoming, Payout, Stake, false, false, previous, PreviousAmount, hasVoid: false));

            // After an earlier Void everything is booked net of the stake.
            Assert.Equal((Payout - Stake, -PreviousAmount), OperatorAuditReversal.Resolve(incoming, Payout, Stake, false, false, previous, PreviousAmount, hasVoid: true));
        }

        #endregion

        #region GM-815 scenarios

        [Fact]
        public void WonThenWonWithADifferentPayout_ReplacesThePreviousPayout()
        {
            // Defect A. Paid 90, resettled Won at 50: the pre-balance drops by 90 and 50 is booked,
            // a net -40, instead of 50 stacking on top of the 90.
            Assert.Equal((50m, -90m), OperatorAuditReversal.Resolve(BetStatus.Won, 50m, Stake, false, false, BetStatus.Won, 90m, hasVoid: false));
        }

        [Fact]
        public void VoidThenWon_ReclaimsTheRefundInsideTheAmount()
        {
            // Defect B as reported. The 60 is the 30 refund reclaimed plus the 90 payout credited,
            // so a bet placed at 100 - 30 = 70, voided back to 100 and then settled Won ends at 160,
            // the same balance a first-time Won reaches. The 160 the ticket expects for a scenario
            // WITHOUT a placement debit would need -30 + 60 and underpays the real flow by 30.
            Assert.Equal((60m, 0m), OperatorAuditReversal.Resolve(BetStatus.Won, Payout, Stake, false, false, BetStatus.Void, Refund, hasVoid: true));
        }

        [Fact]
        public void WonThenVoid_RefundsTheStakeAndReversesThePayout()
        {
            // The direction the ticket confirmed as already correct.
            Assert.Equal((-60m, 0m), OperatorAuditReversal.Resolve(BetStatus.Void, Refund, Stake, false, false, BetStatus.Won, 90m, hasVoid: false));
        }

        [Fact]
        public void CashedOutTwiceWithTheSamePayout_NetsToZero()
        {
            // A duplicate cashout callback used to credit the payout a second time.
            Assert.Equal((45m, -45m), OperatorAuditReversal.Resolve(BetStatus.CashedOut, 45m, Stake, false, false, BetStatus.CashedOut, 45m, hasVoid: false));
        }

        [Fact]
        public void RunningPlacementRow_IsNeverReversed()
        {
            // The ordinary happy path: the placement debit stands and the payout is credited.
            Assert.Equal((Payout, 0m), OperatorAuditReversal.Resolve(BetStatus.Won, Payout, Stake, false, false, BetStatus.Running, -Stake, hasVoid: false));
        }

        #endregion

        [Theory]
        [InlineData(BetStatus.Waiting, false)]
        [InlineData(BetStatus.Running, false)]
        [InlineData(BetStatus.Won, true)]
        [InlineData(BetStatus.HalfWon, true)]
        [InlineData(BetStatus.Lost, true)]
        [InlineData(BetStatus.HalfLost, true)]
        [InlineData(BetStatus.Void, true)]
        [InlineData(BetStatus.Rejected, false)]
        [InlineData(BetStatus.Cancelled, false)]
        [InlineData(BetStatus.CashedOut, true)]
        public void IsSettledOutcome_IsExactlyTheSixReversibleStatuses(BetStatus status, bool expected)
        {
            Assert.Equal(expected, OperatorAuditReversal.IsSettledOutcome(status));
        }
    }
}
