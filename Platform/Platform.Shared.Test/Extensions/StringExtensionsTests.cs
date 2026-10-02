using System;
using Platform.Shared.Extensions;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    // Covers StringExtensions, which parses the pipe-delimited OG (operator gateway) parameter
    // string and maps bet status names into the OG vocabulary.
    //
    // The OG parameter format is "value|currencyCode|gameCode". The parsers disagree about how
    // many segments they require: ToOGParam and GetCurrencyCode return null when there are too
    // few, while GetActualValue and ConvertToUnicodeEscape have no null guard at all and throw on
    // a null input. Both behaviours are pinned below so a future guard is a deliberate change.
    public class StringExtensionsTests
    {
        [Fact]
        public void ToOGParam_ThreeSegments_MapsCurrencyCodeAndGameCode()
        {
            var result = "100.50|USD|SLOT01".ToOGParam();

            Assert.NotNull(result);
            Assert.Equal("USD", result.OGCurrencyCode);
            Assert.Equal("SLOT01", result.GameCode);
        }

        [Fact]
        public void ToOGParam_MoreThanThreeSegments_UsesFirstThree()
        {
            var result = "100.50|USD|SLOT01|extra".ToOGParam();

            Assert.NotNull(result);
            Assert.Equal("USD", result.OGCurrencyCode);
            Assert.Equal("SLOT01", result.GameCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("100.50")]           // one segment
        [InlineData("100.50|USD")]       // two segments: the length <= 2 guard rejects it
        public void ToOGParam_FewerThanThreeSegments_ReturnsNull(string input)
        {
            Assert.Null(input.ToOGParam());
        }

        [Theory]
        [InlineData("CANCELLED", "Rollbacked")]
        [InlineData("cancelled", "Rollbacked")]   // comparison is case-insensitive via ToUpper
        [InlineData("Cancelled", "Rollbacked")]
        [InlineData("VOID", "Cancelled")]
        [InlineData("void", "Cancelled")]
        public void ToOGStatus_MappedStatuses_TranslateToOGVocabulary(string input, string expected)
        {
            Assert.Equal(expected, input.ToOGStatus());
        }

        [Theory]
        [InlineData("Won", "Won")]
        [InlineData("Lost", "Lost")]
        [InlineData("Settled", "Settled")]
        public void ToOGStatus_UnmappedStatus_ReturnsInputUnchanged(string input, string expected)
        {
            Assert.Equal(expected, input.ToOGStatus());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ToOGStatus_NullOrEmpty_ReturnsEmptyString(string input)
        {
            // Note the asymmetry with ToOGParam, which returns null for the same inputs.
            Assert.Equal(string.Empty, input.ToOGStatus());
        }

        [Fact]
        public void GetCurrencyCode_ReturnsSecondSegment()
        {
            Assert.Equal("USD", "100.50|USD|SLOT01".GetCurrencyCode());
            Assert.Equal("USD", "100.50|USD".GetCurrencyCode());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("100.50")]   // no separator, so no second segment
        public void GetCurrencyCode_NoSecondSegment_ReturnsNull(string input)
        {
            Assert.Null(input.GetCurrencyCode());
        }

        [Fact]
        public void GetCurrencyCode_EmptySecondSegment_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, "100.50|".GetCurrencyCode());
        }

        [Theory]
        [InlineData("100.50|USD|SLOT01", "100.50")]
        [InlineData("100.50", "100.50")]      // no separator: the whole string is the value
        [InlineData("", "")]
        public void GetActualValue_ReturnsFirstSegment(string input, string expected)
        {
            Assert.Equal(expected, input.GetActualValue());
        }

        [Fact]
        public void GetActualValue_NullInput_Throws()
        {
            // Unlike its siblings, GetActualValue has no null guard. Pinning the current behaviour
            // so that adding one is a deliberate change rather than an accident.
            string nullInput = null!;

            Assert.Throws<NullReferenceException>(() => nullInput.GetActualValue());
        }

        [Theory]
        [InlineData("a+b", @"a\u002Bb")]
        [InlineData("it's", @"it\u0027s")]
        [InlineData("caf´", @"caf\u00B4")]
        [InlineData("a&b", @"a\u0026b")]
        public void ConvertToUnicodeEscape_EscapesTheProblemCharacters(string input, string expected)
        {
            Assert.Equal(expected, input.ConvertToUnicodeEscape());
        }

        [Fact]
        public void ConvertToUnicodeEscape_EscapesEveryOccurrence()
        {
            Assert.Equal(@"a\u002Bb\u002Bc", "a+b+c".ConvertToUnicodeEscape());
        }

        [Theory]
        [InlineData("plain text")]
        [InlineData("")]
        public void ConvertToUnicodeEscape_NothingToEscape_ReturnsInputUnchanged(string input)
        {
            Assert.Equal(input, input.ConvertToUnicodeEscape());
        }

        [Fact]
        public void ConvertToUnicodeEscape_NullInput_Throws()
        {
            string nullInput = null!;

            Assert.Throws<NullReferenceException>(() => nullInput.ConvertToUnicodeEscape());
        }

        [Theory]
        [InlineData("abcdefgh", "****efgh")]
        [InlineData("1234567890123456", "************3456")]
        public void Mask_LongerThanSuffix_KeepsLastFourChars(string input, string expected)
        {
            Assert.Equal(expected, input.Mask());
        }

        [Theory]
        [InlineData("a", "*")]
        [InlineData("abcd", "****")]
        public void Mask_AtOrShorterThanSuffixLength_MasksEntireInput(string input, string expected)
        {
            Assert.Equal(expected, input.Mask());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Mask_NullOrEmpty_ReturnsInputUnchanged(string input)
        {
            Assert.Equal(input, input.Mask());
        }

        [Fact]
        public void Mask_CustomSuffixLength_KeepsThatManyChars()
        {
            Assert.Equal("*******890", "1234567890".Mask(3));
        }
    }
}
