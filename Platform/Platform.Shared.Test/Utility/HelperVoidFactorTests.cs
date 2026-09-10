using System;
using Platform.Shared.Enums;
using Platform.Shared.Utility;
using Xunit;

namespace Platform.Shared.Test.Utility
{
    // Covers both Helper.GetVoidFactor overloads. They are mirror implementations over two
    // different enums - BetStatus and SettlementStatus - which is exactly the shape that drifts:
    // a status added to one enum and handled in one overload, forgotten in the other.
    //
    // Both map Won and Lost to Zero, Void to One, HalfWon and HalfLost to Half, and everything
    // else to Zero via the fall-through. Note that the fall-through and the explicit Won/Lost
    // branch return the same value, so an unhandled status is indistinguishable from a settled
    // one. That is pinned below rather than assumed.
    public class HelperVoidFactorTests
    {
        [Theory]
        [InlineData(BetStatus.Won, VoidFactor.Zero)]
        [InlineData(BetStatus.Lost, VoidFactor.Zero)]
        [InlineData(BetStatus.Void, VoidFactor.One)]
        [InlineData(BetStatus.HalfWon, VoidFactor.Half)]
        [InlineData(BetStatus.HalfLost, VoidFactor.Half)]
        public void GetVoidFactor_BetStatus_MapsTheHandledStatuses(BetStatus status, VoidFactor expected)
        {
            Assert.Equal(expected, Helper.GetVoidFactor(status));
        }

        [Theory]
        [InlineData(BetStatus.Waiting)]
        [InlineData(BetStatus.Running)]
        [InlineData(BetStatus.Rejected)]
        [InlineData(BetStatus.Cancelled)]
        [InlineData(BetStatus.CashedOut)]
        public void GetVoidFactor_BetStatus_UnhandledStatusFallsThroughToZero(BetStatus status)
        {
            // Cancelled is the interesting one: it is void-like in most of the codebase but is
            // not treated as void here.
            Assert.Equal(VoidFactor.Zero, Helper.GetVoidFactor(status));
        }

        [Theory]
        [InlineData(SettlementStatus.Won, VoidFactor.Zero)]
        [InlineData(SettlementStatus.Lost, VoidFactor.Zero)]
        [InlineData(SettlementStatus.Void, VoidFactor.One)]
        [InlineData(SettlementStatus.HalfWon, VoidFactor.Half)]
        [InlineData(SettlementStatus.HalfLost, VoidFactor.Half)]
        public void GetVoidFactor_SettlementStatus_MapsTheHandledStatuses(SettlementStatus status, VoidFactor expected)
        {
            Assert.Equal(expected, Helper.GetVoidFactor(status));
        }

        [Theory]
        [InlineData(SettlementStatus.Pending)]
        [InlineData(SettlementStatus.Cancelled)]
        public void GetVoidFactor_SettlementStatus_UnhandledStatusFallsThroughToZero(SettlementStatus status)
        {
            Assert.Equal(VoidFactor.Zero, Helper.GetVoidFactor(status));
        }

        [Fact]
        public void GetVoidFactor_BothOverloadsAgreeOnTheStatusesTheyShare()
        {
            // The guard against the two implementations drifting apart.
            Assert.Equal(Helper.GetVoidFactor(BetStatus.Won), Helper.GetVoidFactor(SettlementStatus.Won));
            Assert.Equal(Helper.GetVoidFactor(BetStatus.Lost), Helper.GetVoidFactor(SettlementStatus.Lost));
            Assert.Equal(Helper.GetVoidFactor(BetStatus.Void), Helper.GetVoidFactor(SettlementStatus.Void));
            Assert.Equal(Helper.GetVoidFactor(BetStatus.HalfWon), Helper.GetVoidFactor(SettlementStatus.HalfWon));
            Assert.Equal(Helper.GetVoidFactor(BetStatus.HalfLost), Helper.GetVoidFactor(SettlementStatus.HalfLost));
            Assert.Equal(Helper.GetVoidFactor(BetStatus.Cancelled), Helper.GetVoidFactor(SettlementStatus.Cancelled));
        }

        [Fact]
        public void GetVoidFactor_ValueOutsideTheEnum_ReturnsZero()
        {
            Assert.Equal(VoidFactor.Zero, Helper.GetVoidFactor((BetStatus)9999));
            Assert.Equal(VoidFactor.Zero, Helper.GetVoidFactor((SettlementStatus)9999));
        }
    }
}
