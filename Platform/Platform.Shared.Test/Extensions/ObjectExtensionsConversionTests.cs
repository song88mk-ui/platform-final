using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Platform.Shared.Extensions;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    // Covers the ObjectExtensions members that ObjectExtensionsTest leaves untested: ToDictionary,
    // ToJsonFormat, ToStringOG, the string overload of ToSignOG, and the parts of
    // NormalizeEmptyStrings beyond the null and whitespace cases.
    //
    // These build the payload that is signed and sent to the operator gateway, so the exact
    // string shape is a wire contract: key=value pairs joined by "&", sorted, with the signature
    // key excluded. A change to the ordering, the separator, or the exclusion silently invalidates
    // every signature the operator checks.
    public class ObjectExtensionsConversionTests
    {
        // Mirrors the real OG message models, which name the signature property with
        // [JsonPropertyName("signature")]. That attribute is what makes the exclusion in
        // ToStringOG work, because the filter compares against the literal lowercase key.
        private class Payload
        {
            public string PlayerCode { get; set; }
            public decimal Amount { get; set; }

            [JsonPropertyName("signature")]
            public string Signature { get; set; }
        }

        // The same payload without the attribute, to pin what happens when a model does not
        // follow the convention.
        private class PayloadWithPascalCaseSignature
        {
            public string PlayerCode { get; set; }
            public decimal Amount { get; set; }
            public string Signature { get; set; }
        }

        private class Nested
        {
            public string Name { get; set; }
            public Payload Inner { get; set; }
        }

        private class WithReadOnly
        {
            public string Value { get; set; }
            public string Derived => Value + "-derived";
        }

        #region ToJsonFormat / ToDictionary

        [Fact]
        public void ToJsonFormat_SerialisesPropertyNamesAsDeclared()
        {
            var json = new Payload { PlayerCode = "P1", Amount = 10.5m, Signature = "sig" }.ToJsonFormat();

            Assert.Contains("\"PlayerCode\":\"P1\"", json);
            Assert.Contains("\"Amount\":10.5", json);
        }

        [Fact]
        public void ToJsonFormat_ToLower_LowercasesKeysAndValuesAlike()
        {
            // The flag lowercases the whole serialised string, not just the keys, so a value's
            // casing is lost too. Pinned because it is a trap for anyone signing the output.
            var json = new Payload { PlayerCode = "PlayerOne", Amount = 10.5m }.ToJsonFormat(toLower: true);

            Assert.Contains("\"playercode\":\"playerone\"", json);
        }

        [Fact]
        public void ToDictionary_ReturnsOneEntryPerProperty()
        {
            var dictionary = new Payload { PlayerCode = "P1", Amount = 10.5m, Signature = "sig" }.ToDictionary();

            Assert.Equal(3, dictionary.Count);
            Assert.True(dictionary.ContainsKey("PlayerCode"));
            Assert.True(dictionary.ContainsKey("Amount"));

            // The key follows [JsonPropertyName], not the CLR property name.
            Assert.True(dictionary.ContainsKey("signature"));
            Assert.False(dictionary.ContainsKey("Signature"));
        }

        [Fact]
        public void ToDictionary_ToLower_LowercasesTheKeys()
        {
            var dictionary = new Payload { PlayerCode = "P1", Amount = 10.5m }.ToDictionary(toLower: true);

            Assert.True(dictionary.ContainsKey("playercode"));
            Assert.False(dictionary.ContainsKey("PlayerCode"));
        }

        #endregion

        #region ToStringOG

        [Fact]
        public void ToStringOG_JoinsSortedKeyValuePairsWithAmpersand()
        {
            var result = new Payload { PlayerCode = "P1", Amount = 10.5m, Signature = "sig" }.ToStringOG();

            // Sorted by the whole "key=value" string, and Signature is excluded by key name.
            Assert.Equal("Amount=10.5&PlayerCode=P1", result);
        }

        [Fact]
        public void ToStringOG_PascalCaseSignatureIsNotExcluded()
        {
            // The filter compares against the literal lowercase "signature", so a model that
            // does not carry [JsonPropertyName("signature")] signs its own signature field.
            // That produces a signature the operator cannot reproduce. Pinned as a trap for any
            // new OG model that forgets the attribute.
            var result = new PayloadWithPascalCaseSignature { PlayerCode = "P1", Amount = 1m, Signature = "sig" }.ToStringOG();

            Assert.Contains("Signature=sig", result);
        }

        [Fact]
        public void ToStringOG_NullPropertyBecomesEmptyValue()
        {
            var result = new Payload { PlayerCode = null, Amount = 1m }.ToStringOG();

            Assert.Contains("PlayerCode=", result);
            Assert.DoesNotContain("PlayerCode=null", result);
        }

        [Fact]
        public void ToStringOG_IsIndependentOfPropertyDeclarationOrder()
        {
            // The sort is what makes the signature reproducible on the operator's side.
            var a = new Payload { PlayerCode = "P1", Amount = 2m }.ToStringOG();
            var b = new Payload { Amount = 2m, PlayerCode = "P1" }.ToStringOG();

            Assert.Equal(a, b);
        }

        #endregion

        #region ToSignOG

        [Fact]
        public void ToSignOG_String_IsLowercaseMd5OfDataAndKey()
        {
            var signature = "Amount=10.5&PlayerCode=P1".ToSignOG("secret");

            Assert.Equal(32, signature.Length);
            Assert.Equal(signature.ToLowerInvariant(), signature);
        }

        [Fact]
        public void ToSignOG_String_MatchesTheUppercaseHashLowercased()
        {
            const string data = "Amount=10.5&PlayerCode=P1";

            Assert.Equal(data.ToMD5Hash("secret").ToLowerInvariant(), data.ToSignOG("secret"));
        }

        [Fact]
        public void ToSignOG_Object_EqualsSigningItsToStringOG()
        {
            // The object overload must stay equivalent to signing the string form, because both
            // are used at different call sites against the same operator.
            var model = new Payload { PlayerCode = "P1", Amount = 10.5m, Signature = "ignored" };

            Assert.Equal(model.ToStringOG().ToSignOG("secret"), model.ToSignOG("secret"));
        }

        [Fact]
        public void ToSignOG_Object_DifferentKeyProducesDifferentSignature()
        {
            var model = new Payload { PlayerCode = "P1", Amount = 10.5m };

            Assert.NotEqual(model.ToSignOG("secret"), model.ToSignOG("other"));
        }

        #endregion

        #region NormalizeEmptyStrings

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void NormalizeEmptyStrings_BlankString_ReturnsNull(string value)
        {
            Assert.Null(ObjectExtensions.NormalizeEmptyStrings(value));
        }

        [Fact]
        public void NormalizeEmptyStrings_NonBlankString_ReturnsItUnchanged()
        {
            Assert.Equal("value", ObjectExtensions.NormalizeEmptyStrings("value"));
        }

        [Fact]
        public void NormalizeEmptyStrings_PrimitivesAndValueTypes_PassThroughUntouched()
        {
            Assert.Equal(7, ObjectExtensions.NormalizeEmptyStrings(7));
            Assert.Equal(true, ObjectExtensions.NormalizeEmptyStrings(true));
            Assert.Equal(7.5m, ObjectExtensions.NormalizeEmptyStrings(7.5m));
            Assert.Equal(DayOfWeek.Monday, ObjectExtensions.NormalizeEmptyStrings(DayOfWeek.Monday));

            var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.Equal(timestamp, ObjectExtensions.NormalizeEmptyStrings(timestamp));
        }

        [Fact]
        public void NormalizeEmptyStrings_Collection_NormalisesEachItem()
        {
            var input = new List<string> { "keep", "  ", "" };

            var result = Assert.IsType<List<object>>(ObjectExtensions.NormalizeEmptyStrings(input));

            Assert.Equal(3, result.Count);
            Assert.Equal("keep", result[0]);
            Assert.Null(result[1]);
            Assert.Null(result[2]);
        }

        [Fact]
        public void NormalizeEmptyStrings_Array_BecomesAList()
        {
            // A collection comes back as List<object> regardless of the input type, so the
            // returned object is not assignable back to the property it came from.
            var result = ObjectExtensions.NormalizeEmptyStrings(new[] { "a", " " });

            Assert.IsType<List<object>>(result);
        }

        [Fact]
        public void NormalizeEmptyStrings_ComplexObject_BlanksItsStringPropertiesInPlace()
        {
            var model = new Payload { PlayerCode = "   ", Amount = 5m, Signature = "sig" };

            var result = ObjectExtensions.NormalizeEmptyStrings(model);

            Assert.Same(model, result);
            Assert.Null(model.PlayerCode);
            Assert.Equal(5m, model.Amount);
            Assert.Equal("sig", model.Signature);
        }

        [Fact]
        public void NormalizeEmptyStrings_NestedObject_IsNormalisedRecursively()
        {
            var model = new Nested { Name = " ", Inner = new Payload { PlayerCode = "", Amount = 1m } };

            ObjectExtensions.NormalizeEmptyStrings(model);

            Assert.Null(model.Name);
            Assert.NotNull(model.Inner);
            Assert.Null(model.Inner.PlayerCode);
        }

        [Fact]
        public void NormalizeEmptyStrings_ReadOnlyProperty_IsSkipped()
        {
            // The CanRead && CanWrite guard means a computed property is neither read nor written.
            var model = new WithReadOnly { Value = " " };

            ObjectExtensions.NormalizeEmptyStrings(model);

            Assert.Null(model.Value);
            Assert.Equal("-derived", model.Derived);
        }

        #endregion
    }
}
