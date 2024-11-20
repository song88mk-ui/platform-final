using Platform.Shared.Models;

namespace Platform.Shared.Extensions
{
    public static class StringExtensions
    {

        public static OGRequestDTO ToOGParam(this string input)
        {
            if(string.IsNullOrEmpty(input)) 
                return null;

            var ogParam = input.Split('|');
            if (ogParam.Length <= 2)
                return null;

            var ogCurrencyCode = ogParam.Length >= 2 ? ogParam[1] : string.Empty;
            var gameCode = ogParam.Length >= 3 ? ogParam[2] : string.Empty;

            return new OGRequestDTO
            {
                GameCode = gameCode,
                OGCurrencyCode = ogCurrencyCode
            };
        }

        public static string ToOGStatus(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if(input.ToUpper().Equals("CANCELLED"))
                return "Rollbacked";

            if (input.ToUpper().Equals("VOID"))
                return "Cancelled";

            return input;
        }

        public static string GetCurrencyCode(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var result = input.Split('|');
            if (result.Length <= 1)
                return null;

            return result.Length >= 2 ? result[1] : string.Empty;
        }

        public static string GetActualValue(this string input)
        {
            var result = input.Split('|');
            if (result.Length <= 0)
                return null;

            return result.Length >= 1 ? result[0] : string.Empty;
        }

        public static string ConvertToUnicodeEscape(this string input)
        {
            var charArrays = new char[] { '+', '\'', '´', '&' };

            foreach (var item in charArrays)
            {
                input = input.Replace($"{item}", item.EscapeSequence());
            }

            return input;

            //return input.Replace("+", @"\u002B").Replace("´", @"\u00B4");
        }

        private static string EscapeSequence(this char c) => $@"\u{(int)c:X4}";

    }
}
