using Platform.Shared.Utility;
using Xunit;

namespace Platform.Shared.Test.Utility
{
    // Pins the C# hash to the actual SportsbookDynamic.SOURCE_HASH values observed in the
    // Translation DB (CONVERT(varchar(64), HASHBYTES('SHA2_256', SOURCE_TEXT), 2)).
    // If this ever fails, content-keyed lookups would miss — do not "fix" by changing the
    // expected values; investigate the encoding/casing instead.
    public class TranslationHashingTests
    {
        [Theory]
        [InlineData("Match Winner",
            "3F2305F5C8FAA052E1471B5A1A498515804884E9C6F33699550814CEDABE1483")]
        [InlineData("1st Innings Away Team Next Delivery Total Runs",
            "2E5F0A680685D433D8728135874ED7A94144A22489E19BBACF7FD6AC35BB3FAB")]
        [InlineData("Next Over Away Team Total Runs Over/Under 1st Inns",
            "8858ACBDC198CBD782B5281B7560EC26CA0B83CEAF2375F7E2DF6311B243AE83")]
        [InlineData("Competitor One Numbered Set Total Games Over/Under",
            "97EE51156A627827F353E15DEE3F8438AEC772DA9EE044AFEF2DFC547BA0E28E")]
        [InlineData("Competitor Two Numbered Set Total Games Over/Under",
            "C1E3CAAE86C6ACACB84D1CB61801F27EAB9CECEADF59A96F16E63450192456FD")]
        public void SourceHash_MatchesDatabaseComputedColumn(string sourceText, string expected)
        {
            Assert.Equal(expected, TranslationHashing.SourceHash(sourceText));
        }

        [Fact]
        public void SourceHash_IsUppercase64CharHex()
        {
            var hash = TranslationHashing.SourceHash("Money Line");

            Assert.Equal(64, hash.Length);
            Assert.Equal(hash.ToUpperInvariant(), hash);
            Assert.DoesNotContain("0x", hash);
        }

        [Fact]
        public void SourceHash_ReturnsNull_ForNull()
        {
            Assert.Null(TranslationHashing.SourceHash(null));
        }
    }
}
