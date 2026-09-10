using System.Collections.Generic;
using Platform.Shared.Extensions;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    // Covers ListExtensions.GetDataString, which builds the ampersand-joined, sorted key string
    // used as the signing payload for operator-gateway calls. The sort is what makes the signature
    // independent of the caller's insertion order, so both sides of the integration must agree on
    // it: losing the OrderBy would break signature verification without any local test failing.
    public class ListExtensionsTests
    {
        [Fact]
        public void GetDataString_JoinsWithAmpersandAndNoTrailingSeparator()
        {
            var list = new List<string> { "a", "b", "c" };

            Assert.Equal("a&b&c", list.GetDataString());
        }

        [Fact]
        public void GetDataString_SortsRegardlessOfInsertionOrder()
        {
            var inserted = new List<string> { "c", "a", "b" };

            Assert.Equal("a&b&c", inserted.GetDataString());
        }

        [Fact]
        public void GetDataString_IsIndependentOfInsertionOrder()
        {
            var first = new List<string> { "amount=10", "currency=USD", "betId=7" };
            var second = new List<string> { "betId=7", "amount=10", "currency=USD" };

            Assert.Equal(first.GetDataString(), second.GetDataString());
        }

        [Fact]
        public void GetDataString_SingleItem_ReturnsItemWithoutSeparator()
        {
            Assert.Equal("only", new List<string> { "only" }.GetDataString());
        }

        [Fact]
        public void GetDataString_EmptyList_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, new List<string>().GetDataString());
        }

        [Fact]
        public void GetDataString_NullList_ReturnsEmptyString()
        {
            List<string> nullList = null!;

            Assert.Equal(string.Empty, nullList.GetDataString());
        }

        [Fact]
        public void GetDataString_KeepsDuplicates()
        {
            var list = new List<string> { "a", "a" };

            Assert.Equal("a&a", list.GetDataString());
        }

        [Fact]
        public void GetDataString_EmptyStringEntry_ProducesLeadingSeparator()
        {
            // An empty entry sorts first and leaves a leading "&", which changes the signed payload.
            // Pinning it so a caller passing an unset value sees a defined result.
            var list = new List<string> { "b", string.Empty };

            Assert.Equal("&b", list.GetDataString());
        }
    }
}
