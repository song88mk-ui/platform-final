using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Platform.Shared.Extensions
{
    public static class DecimalExtensions
    {
        public static string AmountFormat = "N2";
        public static int DecimalPlaces = 2;

        public static string Truncate(this decimal value)
        {
            try
            {
                return (Math.Truncate(value * (decimal)Math.Pow(10, DecimalPlaces)) / (decimal)Math.Pow(10, DecimalPlaces)).ToString(AmountFormat);
            }
            catch
            {
                return "0";
            }
        }

        public static decimal Parse(this decimal value)
        {
            try
            {
                return decimal.Parse(value.ToString("G29"));
            }
            catch
            {
                return 0m;
            }
        }
    }
}
