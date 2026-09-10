using System.Collections.Generic;
using Platform.Shared.Extensions;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    // Covers the nested-value path of ObjectExtensions.ConvertObjectToString, reached only when a
    // payload property is itself an object or an array. Scalar properties take the plain
    // ToString path, so the whole branch stays untested unless a payload has structure.
    //
    // Two rewrites happen on that branch, and both are wire-format decisions the operator's
    // signature check depends on:
    //
    //   "/"  becomes  "\/"      - the operator escapes forward slashes in its own JSON
    //   "\"\"" becomes "null"   - an empty string is signed as a null, not as an empty value
    //
    // Neither is reachable through a flat payload, which is why they survived the first pass.
    public class ObjectExtensionsNestedValueTests
    {
        private class WithNestedObject
        {
            public string Id { get; set; }
            public Inner Detail { get; set; }
        }

        private class WithNestedArray
        {
            public string Id { get; set; }
            public List<string> Tags { get; set; }
        }

        public class Inner
        {
            public string Url { get; set; }
            public string Note { get; set; }
        }

        [Fact]
        public void ToStringOG_NestedObject_IsSerialisedInline()
        {
            var model = new WithNestedObject { Id = "A", Detail = new Inner { Url = "x", Note = "n" } };

            var result = model.ToStringOG();

            Assert.Contains("Detail={", result);
            Assert.Contains("\"Url\":\"x\"", result);
        }

        [Fact]
        public void ToStringOG_NestedObject_EscapesForwardSlashes()
        {
            var model = new WithNestedObject { Id = "A", Detail = new Inner { Url = "a/b/c", Note = "n" } };

            var result = model.ToStringOG();

            Assert.Contains(@"a\/b\/c", result);
            Assert.DoesNotContain("\"Url\":\"a/b", result);
        }

        [Fact]
        public void ToStringOG_NestedObject_EmptyStringBecomesNull()
        {
            var model = new WithNestedObject { Id = "A", Detail = new Inner { Url = "x", Note = "" } };

            var result = model.ToStringOG();

            Assert.Contains("\"Note\":null", result);
            Assert.DoesNotContain("\"Note\":\"\"", result);
        }

        [Fact]
        public void ToStringOG_NestedArray_IsSerialisedInline()
        {
            var model = new WithNestedArray { Id = "A", Tags = new List<string> { "one", "two" } };

            var result = model.ToStringOG();

            Assert.Contains("Tags=[", result);
            Assert.Contains("\"one\"", result);
        }

        [Fact]
        public void ToStringOG_NestedArray_EscapesForwardSlashes()
        {
            var model = new WithNestedArray { Id = "A", Tags = new List<string> { "a/b" } };

            var result = model.ToStringOG();

            Assert.Contains(@"a\/b", result);
        }

        [Fact]
        public void ToStringOG_ScalarPropertyIsNotRewritten()
        {
            // The rewrites apply only to object and array values. A slash in a plain string
            // property is signed as-is, which is the asymmetry a caller has to know about.
            var model = new WithNestedArray { Id = "a/b", Tags = new List<string>() };

            var result = model.ToStringOG();

            Assert.Contains("Id=a/b", result);
            Assert.DoesNotContain(@"Id=a\/b", result);
        }

        [Fact]
        public void ToSignOG_NestedObject_SignsTheRewrittenForm()
        {
            // The signature must be taken over the escaped payload, not the raw one, or the
            // operator recomputes a different hash.
            var model = new WithNestedObject { Id = "A", Detail = new Inner { Url = "a/b", Note = "n" } };

            Assert.Equal(model.ToStringOG().ToSignOG("secret"), model.ToSignOG("secret"));
        }
    }
}
