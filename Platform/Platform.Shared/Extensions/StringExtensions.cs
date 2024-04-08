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

            var gameCode = ogParam.Length >= 4 ? ogParam[2] : string.Empty;
            var calledAt = ogParam.Length >= 4 ? Convert.ToInt64(ogParam[1]) : 0;
            var betPlace = ogParam.Length >= 4 ? ogParam[3] : string.Empty;

            return new OGRequestDTO
            {
                BetPlace = betPlace,
                CalledAt = calledAt,
                GameCode = gameCode
            };
        }
    }
}
