using System;
using Platform.Sportsbook.API.Utility;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // GM-771: eSports segments vanished from the Sports Tree between 00:00 and 08:00 UTC+8 because the
    // "now" based day window was not shifted back to UTC. Times below are the real reproduction window
    // from the ticket (player at UTC+8, MinuteOffset=480, just after local midnight).
    public class LocalDayRangeTests
    {
        private static readonly DateTime Sep06_1630Z = new DateTime(2026, 9, 6, 16, 30, 0, DateTimeKind.Utc); // 2026-09-07 00:30 UTC+8

        [Fact]
        public void Compute_Now_Utc8_JustAfterLocalMidnight_StartsAtLocalMidnightInUtc()
        {
            var range = LocalDayRange.Compute(Sep06_1630Z, 480);

            // local day 2026-09-07 runs 2026-09-06 16:00Z .. 2026-09-07 16:00Z
            Assert.Equal(new DateTime(2026, 9, 6, 15, 59, 59, 999, DateTimeKind.Utc), range.Item1);
            Assert.Equal(new DateTime(2026, 9, 7, 16, 0, 0, DateTimeKind.Utc), range.Item2);
        }

        [Fact]
        public void Compute_Now_Utc8_LiveFixtureStartedAfterLocalMidnight_IsInsideWindow()
        {
            // NBA2K fixture that went live at 00:15 UTC+8; it was excluded before the fix because the
            // window start was 2026-09-06 23:59:59.999Z (08:00 local) instead of 15:59:59.999Z.
            var fixtureStartUtc = new DateTime(2026, 9, 6, 16, 15, 0, DateTimeKind.Utc);

            var range = LocalDayRange.Compute(Sep06_1630Z, 480);

            Assert.True(fixtureStartUtc > range.Item1 && fixtureStartUtc < range.Item2);
        }

        [Fact]
        public void Compute_Now_Utc8_BeforeLocalMidnight_UsesCurrentLocalDay()
        {
            var utcNow = new DateTime(2026, 9, 6, 15, 0, 0, DateTimeKind.Utc); // 2026-09-06 23:00 UTC+8

            var range = LocalDayRange.Compute(utcNow, 480);

            Assert.Equal(new DateTime(2026, 9, 5, 15, 59, 59, 999, DateTimeKind.Utc), range.Item1);
            Assert.Equal(new DateTime(2026, 9, 6, 16, 0, 0, DateTimeKind.Utc), range.Item2);
        }

        [Fact]
        public void Compute_Now_ZeroOffset_IsUtcCalendarDay()
        {
            var range = LocalDayRange.Compute(Sep06_1630Z, 0);

            Assert.Equal(new DateTime(2026, 9, 5, 23, 59, 59, 999, DateTimeKind.Utc), range.Item1);
            Assert.Equal(new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc), range.Item2);
        }

        [Fact]
        public void Compute_Now_NegativeOffset_ShiftsForward()
        {
            var utcNow = new DateTime(2026, 9, 7, 3, 0, 0, DateTimeKind.Utc); // 2026-09-06 22:00 UTC-5

            var range = LocalDayRange.Compute(utcNow, -300);

            Assert.Equal(new DateTime(2026, 9, 6, 4, 59, 59, 999, DateTimeKind.Utc), range.Item1);
            Assert.Equal(new DateTime(2026, 9, 7, 5, 0, 0, DateTimeKind.Utc), range.Item2);
        }

        [Fact]
        public void Compute_DateFromRequest_Utc8_UnchangedByFix()
        {
            // Early tab / dailyfixtures path: the request carries the local calendar date. This path
            // already shifted back to UTC before GM-771 and must keep producing the same window.
            var requestedLocalDate = new DateTime(2026, 9, 7);

            var range = LocalDayRange.Compute(requestedLocalDate, 480, dateIsFromRequest: true);

            Assert.Equal(new DateTime(2026, 9, 6, 15, 59, 59, 999), range.Item1);
            Assert.Equal(new DateTime(2026, 9, 7, 16, 0, 0), range.Item2);
        }

        [Fact]
        public void Compute_NowAndRequest_SameLocalDay_ProduceSameWindow()
        {
            // The two entry points describe the same local day and must agree.
            var fromNow = LocalDayRange.Compute(Sep06_1630Z, 480);
            var fromRequest = LocalDayRange.Compute(new DateTime(2026, 9, 7), 480, dateIsFromRequest: true);

            Assert.Equal(fromRequest.Item1, fromNow.Item1);
            Assert.Equal(fromRequest.Item2, fromNow.Item2);
        }
    }
}
