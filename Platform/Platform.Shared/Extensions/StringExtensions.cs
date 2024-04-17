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

            var gameCode = ogParam.Length >= 3 ? ogParam[2] : string.Empty;
            var calledAt = ogParam.Length >= 3 ? Convert.ToInt64(ogParam[1]) : 0;

            return new OGRequestDTO
            {
                CalledAt = calledAt,
                GameCode = gameCode
            };
        }
    }
}
