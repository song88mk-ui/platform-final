using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Utility
{
    public static class Formatting
    {
        public static CurrencyCode GetCurrencyCode(string currencyCode) => currencyCode.ToUpper() == "USDT" ? CurrencyCode.USD : (CurrencyCode)Enum.Parse(typeof(CurrencyCode), currencyCode);
    }
}
