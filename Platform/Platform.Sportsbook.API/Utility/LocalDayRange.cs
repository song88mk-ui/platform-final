using System;

namespace Platform.Sportsbook.API.Utility
{
    // Computes the UTC bounds of a player's local calendar day so they can be compared against
    // Fixture.FiStDa, which is stored in UTC. Extracted from SportsbookRepository.GetDateTimeRange2
    // so the boundary math is unit-testable (GM-771).
    public static class LocalDayRange
    {
        // utcDate is either UTC now (dateIsFromRequest = false) or the local calendar date supplied on
        // the request (dateIsFromRequest = true). minuteUTCOffset is the player's offset from UTC in
        // minutes (480 for UTC+8). Returns (start, end) where start is one millisecond before local
        // midnight and end is the next local midnight, both expressed in UTC. Callers filter with
        // FiStDa > start && FiStDa < end.
        public static Tuple<DateTime, DateTime> Compute(DateTime utcDate, int minuteUTCOffset, bool dateIsFromRequest = false)
        {
            var localReferenceDate = dateIsFromRequest ? utcDate : utcDate.AddMinutes(minuteUTCOffset);

            // The day boundaries are local midnights, so they must be shifted back to UTC on both paths.
            // Before GM-771 the shift only ran for request-supplied dates, which moved the "now" based
            // window start to 08:00 local for UTC+8 and dropped every fixture starting before that.
            var start = localReferenceDate.Date.AddMilliseconds(-1).AddMinutes(-minuteUTCOffset);
            var end = localReferenceDate.Date.AddDays(1).AddMinutes(-minuteUTCOffset);

            return Tuple.Create(start, end);
        }
    }
}
