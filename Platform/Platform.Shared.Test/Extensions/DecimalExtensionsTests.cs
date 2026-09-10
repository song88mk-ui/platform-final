using System;
using System.Globalization;
using System.Threading;
using Platform.Shared.Extensions;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    // Covers DecimalExtensions.Truncate and Parse, used to format and normalise money values.
    //
    // Two hazards are pinned here deliberately:
    //  1. Truncate formats with "N2", which is culture-sensitive. Under an invariant or en-US
    //     culture "1,234.56" uses a comma group separator and a dot decimal separator; under a
    //     de-DE culture the same value formats as "1.234,56". These tests fix the culture so the
    //     expectations are deterministic, and the culture dependency is a real property of the
    //     production code, not a test artefact.
    //  2. Both methods wrap their body in a bare catch that returns "0" / 0m. A genuine overflow
    //     therefore turns into a silent zero on a money path. The tests below record the current
    //     behaviour; see the empty-catch findings in docs/ArchitectureRisks.md (R-9).
    //
    // Note: DecimalExtensions.AmountFormat and DecimalPlaces are public mutable statics. No test
    // here mutates them, because xunit runs test classes in parallel and a mutation would leak
    // into unrelated tests. The mutability itself is a defect, not a behaviour worth pinning.
    public class DecimalExtensionsTests : IDisposable
    {
        private readonly CultureInfo _originalCulture;

        public DecimalExtensionsTests()
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public void Dispose() => Thread.CurrentThread.CurrentCulture = _originalCulture;

        [Theory]
        [InlineData(1.239, "1.23")]      // truncates, does not round up to 1.24
        [InlineData(1.231, "1.23")]
        [InlineData(1.999, "1.99")]      // truncates, does not round up to 2.00
        [InlineData(1.2, "1.20")]        // pads to two decimal places
        [InlineData(1, "1.00")]
        [InlineData(0, "0.00")]
        public void Truncate_TruncatesToTwoDecimalPlaces_WithoutRounding(double value, string expected)
        {
            Assert.Equal(expected, ((decimal)value).Truncate());
        }

        [Theory]
        [InlineData(-1.239, "-1.23")]    // truncation is toward zero for negatives too
        [InlineData(-1.999, "-1.99")]
        public void Truncate_NegativeValues_TruncatesTowardZero(double value, string expected)
        {
            Assert.Equal(expected, ((decimal)value).Truncate());
        }

        [Fact]
        public void Truncate_LargeValue_AppliesGroupSeparator()
        {
            // "N2" is a numeric format with group separators, so the output is not round-trippable
            // through decimal.Parse under every culture. Callers that persist this string are
            // relying on formatting, not serialisation.
            Assert.Equal("1,234.56", 1234.5678m.Truncate());
        }

        [Fact]
        public void Truncate_ValueTooLargeToScale_ReturnsZeroString()
        {
            // value * 100 overflows decimal, the bare catch swallows the OverflowException and the
            // caller receives "0" for a very large amount. Pinning the current behaviour.
            Assert.Equal("0", decimal.MaxValue.Truncate());
        }

        [Theory]
        [InlineData(1.5)]
        [InlineData(0)]
        [InlineData(-1.5)]
        [InlineData(1234.5678)]
        public void Parse_RoundTripsTheValue(double value)
        {
            Assert.Equal((decimal)value, ((decimal)value).Parse());
        }

        [Fact]
        public void Parse_StripsTrailingZeroScale()
        {
            // "G29" drops the trailing-zero scale that decimal carries in its representation, so
            // 1.50m and 1.5m both come back as 1.5 with scale 1.
            Assert.Equal(0, decimal.Parse("1.5", CultureInfo.InvariantCulture).CompareTo(1.50m.Parse()));
            Assert.Equal("1.5", 1.50m.Parse().ToString(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Parse_MaxValue_RoundTrips()
        {
            Assert.Equal(decimal.MaxValue, decimal.MaxValue.Parse());
        }
    }
}
