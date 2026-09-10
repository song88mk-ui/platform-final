using System;
using Platform.Shared.Extensions;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    // Covers DateTimeExtensions.GetEpochTimestamp and BetOffset.
    //
    // GetEpochTimestamp constructs a DateTimeOffset from a DateTime, which resolves the offset from
    // the value's Kind: Utc gives a zero offset, while Unspecified and Local are interpreted in the
    // machine's local time zone. A caller that passes an Unspecified DateTime therefore gets a
    // different epoch value on a machine in a different time zone. The tests below assert exact
    // values only for Kind.Utc, and pin the time-zone sensitivity separately rather than asserting
    // a number that would only hold on one machine.
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void GetEpochTimestamp_UtcEpoch_ReturnsZero()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Assert.Equal(0L, epoch.GetEpochTimestamp());
        }

        [Fact]
        public void GetEpochTimestamp_UtcValue_ReturnsMilliseconds()
        {
            var value = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Assert.Equal(1767225600000L, value.GetEpochTimestamp());
        }

        [Fact]
        public void GetEpochTimestamp_UtcValue_IncludesMillisecondComponent()
        {
            var value = new DateTime(1970, 1, 1, 0, 0, 1, 500, DateTimeKind.Utc);

            Assert.Equal(1500L, value.GetEpochTimestamp());
        }

        [Fact]
        public void GetEpochTimestamp_UnspecifiedKind_IsInterpretedInLocalTime()
        {
            // Not an exact-value assertion on purpose: the result depends on the machine's time
            // zone. What is asserted is that Unspecified and Utc disagree by exactly the local
            // offset, which documents the hazard for callers that build DateTimes without a Kind.
            var unspecified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var asUtc = DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);

            var expectedOffsetMs = (long)TimeZoneInfo.Local.GetUtcOffset(unspecified).TotalMilliseconds;

            Assert.Equal(asUtc.GetEpochTimestamp() - expectedOffsetMs, unspecified.GetEpochTimestamp());
        }

        [Fact]
        public void GetEpochTimestamp_IsMonotonicInTheInput()
        {
            var earlier = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var later = earlier.AddSeconds(1);

            Assert.True(later.GetEpochTimestamp() > earlier.GetEpochTimestamp());
        }

        [Fact]
        public void BetOffset_SubtractsFourHours()
        {
            var value = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            Assert.Equal(new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc), value.BetOffset());
        }

        [Fact]
        public void BetOffset_CrossesTheDayBoundary()
        {
            // The four-hour shift is what moves a bet placed just after midnight into the previous
            // betting day, which is the behaviour reports depend on.
            var justAfterMidnight = new DateTime(2026, 1, 2, 2, 0, 0, DateTimeKind.Utc);

            Assert.Equal(new DateTime(2026, 1, 1, 22, 0, 0, DateTimeKind.Utc), justAfterMidnight.BetOffset());
        }

        [Fact]
        public void BetOffset_PreservesKind()
        {
            var value = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            Assert.Equal(DateTimeKind.Utc, value.BetOffset().Kind);
        }
    }
}
