using Platform.Shared.Models;
using System;

namespace Platform.Shared.Extensions
{
    public static class StringExtensions
    {

        public static OGRequestDTO ToOGParam(this string input)
        {
            var ogParam = input.Split('|');
            if (ogParam.Length <= 1)
                return null;

            var gameCode = ogParam.Length >= 2 ? ogParam[1] : string.Empty;
            var ogCurrencyCode = ogParam.Length >= 3 ? ogParam[2] : string.Empty;

            return new OGRequestDTO
            {
                GameCode = gameCode,
                OGCurrencyCode = ogCurrencyCode
            };
        }

        public static string ConvertToUnicodeEscape(this string input)
        {
            return input.Replace("+", @"\u002B"); //.Replace("/", @"\u002F")'
        }

        public static string ConvertAccuteToUnicodeEscape(this string input)
        {
            return input.Replace("´", @"\u00B4");
        }
    }

    public static class DateTimeExtensions
    {
        public static long GetEpochTimestamp(this DateTime dateTime)
        {
            DateTimeOffset dto = new DateTimeOffset(dateTime);
            return dto.ToUnixTimeMilliseconds();
        }
    }
}
