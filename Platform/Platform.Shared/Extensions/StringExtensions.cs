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
    }
}
