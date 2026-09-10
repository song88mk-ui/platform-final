using System;
using Platform.Shared.Enums;
using Platform.Shared.Utility;
using Xunit;

namespace Platform.Shared.Test.Utility
{
    // Covers Formatting.GetCurrencyCode, which maps an incoming currency string onto the
    // CurrencyCode enum. The single special case is USDT, the stablecoin, which is folded onto USD
    // because the enum has no USDT member. Everything else goes through Enum.Parse, which is
    // case-sensitive and throws on an unknown code - so an operator sending a lowercase or
    // unrecognised currency gets an exception, not a fallback. Both are pinned here.
    public class FormattingTests
    {
        [Fact]
        public void GetCurrencyCode_Usdt_MapsOntoUsd()
        {
            Assert.Equal(CurrencyCode.USD, Formatting.GetCurrencyCode("USDT"));
        }

        [Theory]
        [InlineData("usdt")]
        [InlineData("Usdt")]
        [InlineData("uSdT")]
        public void GetCurrencyCode_UsdtInAnyCase_MapsOntoUsd(string input)
        {
            // The USDT comparison uppercases first, so it is the one case-insensitive path.
            Assert.Equal(CurrencyCode.USD, Formatting.GetCurrencyCode(input));
        }

        [Theory]
        [InlineData("USD", CurrencyCode.USD)]
        [InlineData("EUR", CurrencyCode.EUR)]
        [InlineData("THB", CurrencyCode.THB)]
        [InlineData("VND", CurrencyCode.VND)]
        [InlineData("ALL", CurrencyCode.ALL)]
        public void GetCurrencyCode_KnownCode_ParsesToEnumMember(string input, CurrencyCode expected)
        {
            Assert.Equal(expected, Formatting.GetCurrencyCode(input));
        }

        [Theory]
        [InlineData("usd")]
        [InlineData("Eur")]
        public void GetCurrencyCode_KnownCodeInWrongCase_Throws(string input)
        {
            // Enum.Parse without ignoreCase, so only the exact uppercase member name parses. This
            // is the asymmetry with the USDT path above.
            Assert.Throws<ArgumentException>(() => Formatting.GetCurrencyCode(input));
        }

        [Theory]
        [InlineData("XYZ")]
        [InlineData("")]
        public void GetCurrencyCode_UnknownCode_Throws(string input)
        {
            Assert.Throws<ArgumentException>(() => Formatting.GetCurrencyCode(input));
        }

        [Fact]
        public void GetCurrencyCode_NumericString_ParsesAsUnderlyingValue()
        {
            // Enum.Parse accepts the numeric form, so "1" silently becomes USD without any code
            // named "1" existing. A caller forwarding a raw database value would not notice.
            Assert.Equal(CurrencyCode.USD, Formatting.GetCurrencyCode("1"));
        }

        [Fact]
        public void GetCurrencyCode_UndeclaredNumericValue_ProducesAnUndefinedEnumValue()
        {
            var result = Formatting.GetCurrencyCode("9999");

            Assert.False(Enum.IsDefined(typeof(CurrencyCode), result));
        }

        [Fact]
        public void GetCurrencyCode_NullInput_Throws()
        {
            // ToUpper() is called before any guard, so null throws rather than returning a default.
            Assert.Throws<NullReferenceException>(() => Formatting.GetCurrencyCode(null));
        }
    }
}
