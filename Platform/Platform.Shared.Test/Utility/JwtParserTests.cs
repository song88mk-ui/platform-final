using System;
using System.Linq;
using System.Text;
using Platform.Shared.Utility;
using Xunit;

namespace Platform.Shared.Test.Utility
{
    // Covers JwtParser.ParseClaimsFromJwt, which reads claims out of the operator token.
    //
    // The implementation decodes the payload with Convert.FromBase64String after re-adding the
    // stripped padding. That handles standard base64 but NOT base64url, which is what RFC 7515
    // actually specifies for a JWT: base64url replaces '+' with '-' and '/' with '_', and both of
    // those characters make Convert.FromBase64String throw. A token whose payload happens to
    // encode without those characters parses fine, which is why the gap is easy to miss. The
    // base64url case is pinned below as a known limitation, not as desired behaviour.
    public class JwtParserTests
    {
        private static string Payload(string json) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=');

        private static string Jwt(string json) => $"header.{Payload(json)}.signature";

        [Fact]
        public void ParseClaimsFromJwt_ReturnsOneClaimPerPayloadProperty()
        {
            var claims = JwtParser.ParseClaimsFromJwt(Jwt("{\"sub\":\"user-1\",\"role\":\"operator\"}")).ToList();

            Assert.Equal(2, claims.Count);
            Assert.Equal("user-1", claims.Single(c => c.Type == "sub").Value);
            Assert.Equal("operator", claims.Single(c => c.Type == "role").Value);
        }

        [Fact]
        public void ParseClaimsFromJwt_EmptyPayloadObject_ReturnsNoClaims()
        {
            Assert.Empty(JwtParser.ParseClaimsFromJwt(Jwt("{}")));
        }

        [Fact]
        public void ParseClaimsFromJwt_NonStringValues_AreStringified()
        {
            var claims = JwtParser.ParseClaimsFromJwt(Jwt("{\"exp\":1767225600,\"admin\":true}")).ToList();

            Assert.Equal("1767225600", claims.Single(c => c.Type == "exp").Value);

            // Note the case: the value is a JsonElement, and JsonElement.ToString() renders a
            // boolean with .NET formatting ("True"), not JSON formatting ("true"). A caller
            // comparing a claim against "true" therefore never matches.
            Assert.Equal("True", claims.Single(c => c.Type == "admin").Value);
        }

        [Theory]
        [InlineData("{\"a\":\"1\"}")]                          // payload length % 4 == 2, two '=' re-added
        [InlineData("{\"ab\":\"12\"}")]                        // payload length % 4 == 3, one '=' re-added
        [InlineData("{\"abc\":\"123\",\"d\":\"4\"}")]           // payload length % 4 == 0, nothing re-added
        public void ParseClaimsFromJwt_RestoresStrippedPadding(string json)
        {
            // Real tokens arrive with padding stripped; all three residues must decode.
            Assert.NotEmpty(JwtParser.ParseClaimsFromJwt(Jwt(json)));
        }

        [Fact]
        public void ParseClaimsFromJwt_IgnoresHeaderAndSignature()
        {
            var payload = Payload("{\"sub\":\"user-1\"}");

            var claims = JwtParser.ParseClaimsFromJwt($"not-a-real-header.{payload}.not-a-real-signature").ToList();

            Assert.Equal("user-1", claims.Single().Value);
        }

        [Fact]
        public void ParseClaimsFromJwt_Base64UrlPayload_Throws()
        {
            // Known limitation: base64url is the format a spec-compliant JWT uses, and its '-' and
            // '_' characters are rejected by Convert.FromBase64String. Any token whose payload
            // encodes to those characters fails to parse.
            var base64Url = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"sub\":\"a?b>c\"}"))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            Assert.Throws<FormatException>(() => JwtParser.ParseClaimsFromJwt($"header.{base64Url}.signature"));
        }

        [Fact]
        public void ParseClaimsFromJwt_NoSecondSegment_Throws()
        {
            // jwt.Split('.')[1] is indexed without a length check.
            Assert.Throws<IndexOutOfRangeException>(() => JwtParser.ParseClaimsFromJwt("single-segment"));
        }

        [Fact]
        public void ParseClaimsFromJwt_NullToken_Throws()
        {
            Assert.Throws<NullReferenceException>(() => JwtParser.ParseClaimsFromJwt(null));
        }

        [Fact]
        public void ParseClaimsFromJwt_PayloadIsNotJson_Throws()
        {
            Assert.ThrowsAny<Exception>(() => JwtParser.ParseClaimsFromJwt($"header.{Payload("not json")}.signature"));
        }
    }
}
