using System;

namespace Platform.Shared.Extensions
{
    public static class DateTimeExtensions
    {
        public static long GetEpochTimestamp(this DateTime dateTime)
        {
            DateTimeOffset dto = new DateTimeOffset(dateTime);
            return dto.ToUnixTimeMilliseconds();
        }
    }
}
