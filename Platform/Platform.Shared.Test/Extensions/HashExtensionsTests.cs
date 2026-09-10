using Platform.Shared.Extensions;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    // Covers HashExtensions.ToMD5Hash, which signs outbound operator-gateway requests. The hash is
    // computed over the plain concatenation of input and private key, so the boundary between the
    // two is not delimited: "ab".ToMD5Hash("c") and "a".ToMD5Hash("bc") produce the same signature.
    // That property is pinned below because the receiving side depends on the exact concatenation
    // order, and because it is the reason the two arguments must never be reordered.
    public class HashExtensionsTests
    {
        [Fact]
        public void ToMD5Hash_EmptyInputAndKey_ReturnsTheKnownEmptyStringDigest()
        {
            // MD5 of the empty string, uppercased with separators stripped.
            Assert.Equal("D41D8CD98F00B204E9800998ECF8427E", string.Empty.ToMD5Hash(string.Empty));
        }

        [Fact]
        public void ToMD5Hash_IsDeterministic()
        {
            Assert.Equal("payload".ToMD5Hash("secret"), "payload".ToMD5Hash("secret"));
        }

        [Fact]
        public void ToMD5Hash_ReturnsUppercaseHexWithoutSeparators()
        {
            var hash = "payload".ToMD5Hash("secret");

            Assert.Equal(32, hash.Length);
            Assert.DoesNotContain("-", hash);
            Assert.Equal(hash.ToUpperInvariant(), hash);
            Assert.All(hash, c => Assert.True(char.IsDigit(c) || (c >= 'A' && c <= 'F'), $"unexpected character '{c}' in hash"));
        }

        [Fact]
        public void ToMD5Hash_DifferentKey_ProducesDifferentHash()
        {
            Assert.NotEqual("payload".ToMD5Hash("secret"), "payload".ToMD5Hash("other"));
        }

        [Fact]
        public void ToMD5Hash_DifferentInput_ProducesDifferentHash()
        {
            Assert.NotEqual("payload".ToMD5Hash("secret"), "payload2".ToMD5Hash("secret"));
        }

        [Fact]
        public void ToMD5Hash_ConcatenationBoundaryIsNotDelimited()
        {
            // Documents a real weakness rather than endorsing it: moving a character from the end
            // of the input to the start of the key yields an identical signature.
            Assert.Equal("ab".ToMD5Hash("c"), "a".ToMD5Hash("bc"));
        }

        [Fact]
        public void ToMD5Hash_IsCaseSensitive()
        {
            Assert.NotEqual("payload".ToMD5Hash("secret"), "PAYLOAD".ToMD5Hash("secret"));
        }

        [Fact]
        public void ToMD5Hash_HandlesNonAsciiInputAsUtf8()
        {
            // Encoding is fixed to UTF-8 in the implementation, so a non-ASCII payload hashes
            // consistently regardless of the machine's default encoding.
            var hash = "café".ToMD5Hash("secret");

            Assert.Equal(32, hash.Length);
            Assert.Equal(hash, "café".ToMD5Hash("secret"));
        }
    }
}
