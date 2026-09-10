using Platform.Shared.Extensions;
using Xunit;

namespace Platform.Shared.Test.Extensions
{
    // Covers IntExtensions.GetVirtualSportIds, IsVirtualSportId and IsESports.
    //
    // All three are unfinished: the ID lists are empty and carry "//todo: append ... here"
    // comments, so every sport currently classifies as neither virtual nor e-sport. These tests
    // record that as the deliberate current state. When IDs are added, the first three tests here
    // are expected to fail and must be updated in the same change - that failure is the point,
    // because it forces the author to state which sports they just reclassified.
    public class IntExtensionsTests
    {
        [Fact]
        public void GetVirtualSportIds_IsStillEmpty()
        {
            Assert.Empty(IntExtensions.GetVirtualSportIds());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(-1)]
        public void IsVirtualSportId_NoIdsConfigured_ReturnsFalseForEverySport(int sportId)
        {
            Assert.False(sportId.IsVirtualSportId());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(-1)]
        public void IsESports_NoIdsConfigured_ReturnsFalseForEverySport(int sportId)
        {
            Assert.False(sportId.IsESports());
        }

        [Fact]
        public void GetVirtualSportIds_ReturnsAFreshListEachCall()
        {
            // A new list is allocated per call, so a caller mutating the result cannot corrupt
            // later callers. Worth pinning: switching to a cached static list would change this.
            var first = IntExtensions.GetVirtualSportIds();
            first.Add(42);

            Assert.Empty(IntExtensions.GetVirtualSportIds());
        }
    }
}
