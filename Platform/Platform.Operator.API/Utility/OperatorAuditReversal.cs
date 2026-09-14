using Platform.Shared.Enums;

namespace Platform.Operator.API.Utility
{
    // The balance movement one OperatorAudit row books for a placement, settlement, cashout or
    // cancellation call, including the RESETTLEMENT reversal that has to happen when the same bet
    // already produced a settled row. Pure: PlatformRepository.AddOrUpdateOperatorAudit loads the
    // previous row and calls this; nothing here touches EF. Extracted under GM-815 so the rules are
    // stated once and every (previous, incoming) BetStatus pair is unit tested
    // (Platform.Operator.API.Test/Utility/OperatorAuditReversalTests.cs).
    //
    // The caller writes the row as PreBalance = balance + PreBalanceAdjustment, Amount = Amount,
    // PostBalance = PreBalance + Amount, so PostBalance == PreBalance + Amount always holds.
    //
    // Two baselines are in play, and that is what made the nested conditionals hard to read. A
    // settled row (Won, HalfWon, Lost, HalfLost, CashedOut) books its payout relative to the balance
    // AFTER the placement debit, so a fresh Won books +payout. A Void refunds the stake, which puts
    // the balance back where it was BEFORE the placement, and every row after a Void is booked
    // against that pre-placement baseline: a Won after a Void is worth payout - stake and a Lost
    // after a Void is worth -stake. Both baselines reach the same balance; they differ only in how
    // the Amount column reads.
    public static class OperatorAuditReversal
    {
        // Statuses whose row is reversed when the bet is settled again. Waiting, Running, Rejected
        // and Cancelled rows are never reversed: a Running row is the placement debit (or a
        // partial-cashout credit), and reversing it would refund the stake on every settlement.
        public static bool IsSettledOutcome(BetStatus status) =>
            status == BetStatus.Won
            || status == BetStatus.HalfWon
            || status == BetStatus.Lost
            || status == BetStatus.HalfLost
            || status == BetStatus.Void
            || status == BetStatus.CashedOut;

        // previous / previousAmount describe the LATEST audit row for the same user and bet, or
        // null / 0 when there is none. hasVoid is true when any row for the bet is a Void.
        public static (decimal Amount, decimal PreBalanceAdjustment) Resolve(
            BetStatus incoming,
            decimal payout,
            decimal stake,
            bool isFreebet,
            bool isPartialCashout,
            BetStatus? previous,
            decimal previousAmount,
            bool hasVoid)
        {
            // Running is the placement debit, unless it is a partial cashout of a still-running bet,
            // which credits the cashout payout. Every other status credits its payout: a Lost carries
            // 0, a Void or Cancelled carries the stake being refunded.
            var amount = payout;
            if (incoming == BetStatus.Running && !isPartialCashout)
                amount = isFreebet ? 0 : stake * -1;

            if (previous == null || !IsSettledOutcome(previous.Value))
                return (amount, 0);

            // The refund stands and the new outcome is booked against the pre-placement baseline, so
            // "reclaim the refund, then credit the payout" collapses into one amount: payout - stake.
            // A repeated Void with the same refund therefore books 0.
            if (previous == BetStatus.Void)
                return (payout - stake, 0);

            // Reverse the previous outcome and refund the stake in one amount. After an earlier Void
            // the previous row was already net of the stake, so the refund is too.
            if (incoming == BetStatus.Void)
                return ((hasVoid ? payout - stake : payout) - previousAmount, 0);

            // Settled to settled, including the SAME status arriving again with a different payout
            // (GM-815: this pair used to be skipped, so a repeated Won paid twice). The previous
            // outcome is reversed through the pre-balance and the new outcome is the amount.
            return (hasVoid ? payout - stake : amount, previousAmount * -1);
        }
    }
}
