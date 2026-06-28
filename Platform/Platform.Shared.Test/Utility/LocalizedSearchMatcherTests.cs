using Platform.Shared.Utility;
using Xunit;

namespace Platform.Shared.Test.Utility
{
    // Covers the localized (non-EN) sportsbook search matching extracted from
    // SportsbookRepository.GetSearchResults. Regression guard for GM-539: localized search
    // previously matched the deprecated MongoDB *Tr arrays (returning no results and, when a
    // base-name fallback matched, throwing on the null array). Matching now runs on the localized
    // name resolved from the SQL translation store; these tests pin the resulting rules.
    public class LocalizedSearchMatcherTests
    {
        // ---- MatchLeague ----

        [Fact]
        public void MatchLeague_ChineseExactPrefix_ReturnsStartsWith()
        {
            // The team shown on the page ("烈焰女子队") must now be findable.
            Assert.Equal(SearchMatchKind.StartsWith,
                LocalizedSearchMatcher.MatchLeague("烈焰女子队", "烈焰女子队"));
        }

        [Fact]
        public void MatchLeague_ChinesePrefix_ReturnsStartsWith()
        {
            Assert.Equal(SearchMatchKind.StartsWith,
                LocalizedSearchMatcher.MatchLeague("英国女子T20锦标赛", "英国"));
        }

        [Fact]
        public void MatchLeague_ChineseMidString_ReturnsContains()
        {
            Assert.Equal(SearchMatchKind.Contains,
                LocalizedSearchMatcher.MatchLeague("英国女子T20锦标赛", "女子"));
        }

        [Fact]
        public void MatchLeague_NoMatch_ReturnsNone()
        {
            Assert.Equal(SearchMatchKind.None,
                LocalizedSearchMatcher.MatchLeague("英国女子T20锦标赛", "篮球"));
        }

        [Fact]
        public void MatchLeague_EnglishFallback_IsCaseInsensitive()
        {
            // When no translation row exists the repository passes the base EN name; matching it
            // must stay case-insensitive (preserves the prior EN behaviour).
            Assert.Equal(SearchMatchKind.Contains,
                LocalizedSearchMatcher.MatchLeague("India Maharaja T20 Trophy", "maharaja"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void MatchLeague_NullOrEmptyName_ReturnsNone(string name)
        {
            // Regression for the 1000/NullReferenceException: a fixture matched via the EN fallback
            // could have no localized name at all. This must not throw.
            Assert.Equal(SearchMatchKind.None,
                LocalizedSearchMatcher.MatchLeague(name, "anything"));
        }

        [Fact]
        public void MatchLeague_EmptySearchText_ReturnsNone()
        {
            Assert.Equal(SearchMatchKind.None,
                LocalizedSearchMatcher.MatchLeague("英国女子T20锦标赛", ""));
        }

        // ---- MatchFixture ----

        [Fact]
        public void MatchFixture_HomeParticipantPrefix_ReturnsStartsWith()
        {
            Assert.Equal(SearchMatchKind.StartsWith,
                LocalizedSearchMatcher.MatchFixture("爱尔兰 vs. 印度", "爱尔兰"));
        }

        [Fact]
        public void MatchFixture_AwayParticipantPrefix_ReturnsStartsWith()
        {
            Assert.Equal(SearchMatchKind.StartsWith,
                LocalizedSearchMatcher.MatchFixture("爱尔兰 vs. 印度", "印度"));
        }

        [Fact]
        public void MatchFixture_LocalizedSeparatorWithoutSpaces_StillSplits()
        {
            // Localized names may drop the spaces around "vs." (e.g. "保加利亚vs.乌克兰").
            Assert.Equal(SearchMatchKind.StartsWith,
                LocalizedSearchMatcher.MatchFixture("保加利亚vs.乌克兰", "乌克兰"));
        }

        [Fact]
        public void MatchFixture_MidParticipantString_ReturnsContains()
        {
            // Substring that is not a participant prefix falls through to a whole-name "contains".
            Assert.Equal(SearchMatchKind.Contains,
                LocalizedSearchMatcher.MatchFixture("曼彻斯特联 vs. 利物浦", "彻斯特"));
        }

        [Fact]
        public void MatchFixture_NoMatch_ReturnsNone()
        {
            Assert.Equal(SearchMatchKind.None,
                LocalizedSearchMatcher.MatchFixture("爱尔兰 vs. 印度", "巴西"));
        }

        [Fact]
        public void MatchFixture_EnglishName_IsCaseInsensitive()
        {
            Assert.Equal(SearchMatchKind.StartsWith,
                LocalizedSearchMatcher.MatchFixture("Ireland vs. India", "IRELAND"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void MatchFixture_NullOrEmptyName_ReturnsNone(string name)
        {
            Assert.Equal(SearchMatchKind.None,
                LocalizedSearchMatcher.MatchFixture(name, "anything"));
        }
    }
}
