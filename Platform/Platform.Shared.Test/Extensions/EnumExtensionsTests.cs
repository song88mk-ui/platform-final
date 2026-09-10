using System.ComponentModel;
using Platform.Shared.Extensions;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    // Covers EnumExtensions.GetDescription, which resolves an enum member's [Description] attribute
    // and falls back to the member name with underscores replaced by spaces. Uses a local enum so
    // the test does not break when a production enum's descriptions are edited.
    public class EnumExtensionsTests
    {
        private enum SampleStatus
        {
            [Description("Awaiting settlement")]
            Pending = 0,

            Settled = 1,

            Partially_Cashed_Out = 2,

            [Description("")]
            EmptyDescription = 3
        }

        [Fact]
        public void GetDescription_MemberWithDescription_ReturnsTheDescription()
        {
            Assert.Equal("Awaiting settlement", SampleStatus.Pending.GetDescription());
        }

        [Fact]
        public void GetDescription_MemberWithoutDescription_ReturnsMemberName()
        {
            Assert.Equal("Settled", SampleStatus.Settled.GetDescription());
        }

        [Fact]
        public void GetDescription_MemberWithUnderscores_ReplacesThemWithSpaces()
        {
            Assert.Equal("Partially Cashed Out", SampleStatus.Partially_Cashed_Out.GetDescription());
        }

        [Fact]
        public void GetDescription_EmptyDescription_ReturnsEmptyStringNotMemberName()
        {
            // The null-coalescing fallback only triggers on a missing attribute, so an explicitly
            // empty [Description("")] wins over the member name.
            Assert.Equal(string.Empty, SampleStatus.EmptyDescription.GetDescription());
        }

        [Fact]
        public void GetDescription_ValueNotDeclaredInTheEnum_ReturnsTheNumericValue()
        {
            // GetField returns null for an undeclared value, so the fallback formats the number.
            // This is the path taken when a database column holds a value the enum does not know.
            Assert.Equal("99", ((SampleStatus)99).GetDescription());
        }
    }
}
