using Platform.Shared.Models;
using System;
using System.Linq;

namespace Platform.Shared.Extensions
{
    public static class DecimalExtensions
    {

        public static decimal RedenominateOG(this decimal input, string currencyCode)
        {
            var highCurrencies = "IDRK,VNDK,MMKK,INRK".Split(",").ToList();
            if(highCurrencies.Contains(currencyCode.ToUpper()))
                return input/1000;
            else
                return input;
        }
    }
}
