using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Sportsbook.API.Utility;
using System.Collections.Generic;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // The rule these tests defend: an untranslated entity renders its original English name, never
    // blank and never a key. Nothing downstream re-checks it, so a regression here shows up as
    // empty fixture, market or selection names in the client.
    public class MongoTranslationResolverTests
    {
        private static List<TeamTranslationMDB> Teams(params (string language, string name)[] entries)
        {
            var list = new List<TeamTranslationMDB>();

            foreach (var entry in entries)
                list.Add(new TeamTranslationMDB { LaCo = entry.language, TeNa = entry.name });

            return list;
        }

        #region FindTranslation

        [Fact]
        public void FindTranslation_ReturnsTextForRequestedLanguage()
        {
            var translations = Teams(("ES", "Fluminense"), ("ZH", "弗盧米嫩塞"));

            Assert.Equal("弗盧米嫩塞", MongoTranslationResolver.FindTranslation(translations, "ZH", t => t.LaCo, t => t.TeNa));
        }

        [Fact]
        public void FindTranslation_MatchesLanguageCaseInsensitively()
        {
            var translations = Teams(("zh", "弗盧米嫩塞"));

            Assert.Equal("弗盧米嫩塞", MongoTranslationResolver.FindTranslation(translations, "ZH", t => t.LaCo, t => t.TeNa));
        }

        [Fact]
        public void FindTranslation_ReturnsNull_WhenLanguageAbsent()
        {
            var translations = Teams(("ES", "Fluminense"));

            Assert.Null(MongoTranslationResolver.FindTranslation(translations, "ZH", t => t.LaCo, t => t.TeNa));
        }

        [Fact]
        public void FindTranslation_ReturnsNull_WhenListIsNullOrEmpty()
        {
            Assert.Null(MongoTranslationResolver.FindTranslation((List<TeamTranslationMDB>)null, "ZH", t => t.LaCo, t => t.TeNa));
            Assert.Null(MongoTranslationResolver.FindTranslation(Teams(), "ZH", t => t.LaCo, t => t.TeNa));
        }

        // An entry whose text is empty is a cleared translation, not a blank name to render.
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void FindTranslation_ReturnsNull_WhenMatchedTextIsEmpty(string text)
        {
            var translations = Teams(("ZH", text));

            Assert.Null(MongoTranslationResolver.FindTranslation(translations, "ZH", t => t.LaCo, t => t.TeNa));
        }

        [Fact]
        public void FindTranslation_SkipsNullEntries()
        {
            var translations = new List<TeamTranslationMDB> { null, new TeamTranslationMDB { LaCo = "ZH", TeNa = "弗盧米嫩塞" } };

            Assert.Equal("弗盧米嫩塞", MongoTranslationResolver.FindTranslation(translations, "ZH", t => t.LaCo, t => t.TeNa));
        }

        #endregion

        #region AddTranslation

        [Fact]
        public void AddTranslation_StoresTranslatedText()
        {
            var map = new Dictionary<string, string>();

            MongoTranslationResolver.AddTranslation(map, "Te:4471", "弗盧米嫩塞");

            Assert.Equal("弗盧米嫩塞", map["Te:4471"]);
        }

        // Leaving the key out is what makes ResolveName fall back to English. Writing an empty
        // string instead would render a blank name.
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void AddTranslation_LeavesKeyOut_WhenNothingToAdd(string translated)
        {
            var map = new Dictionary<string, string>();

            MongoTranslationResolver.AddTranslation(map, "Te:4471", translated);

            Assert.Empty(map);
        }

        #endregion

        #region ResolveName - the English fallback

        [Fact]
        public void ResolveName_ReturnsTranslation_WhenKeyPresent()
        {
            var translations = new Dictionary<string, string> { ["LeId:88"] = "英格蘭超級聯賽" };

            Assert.Equal("英格蘭超級聯賽", MongoTranslationResolver.ResolveName(translations, 88, "LeId", "Premier League"));
        }

        [Fact]
        public void ResolveName_ReturnsEnglishName_WhenKeyMissing()
        {
            var translations = new Dictionary<string, string>();

            Assert.Equal("Premier League", MongoTranslationResolver.ResolveName(translations, 88, "LeId", "Premier League"));
        }

        [Fact]
        public void ResolveName_ReturnsEnglishName_WhenIdIsNull()
        {
            var translations = new Dictionary<string, string> { ["LeId:88"] = "英格蘭超級聯賽" };

            Assert.Equal("Premier League", MongoTranslationResolver.ResolveName(translations, null, "LeId", "Premier League"));
        }

        // The prefix is part of the key, so a league id must not pick up a sport's translation.
        [Fact]
        public void ResolveName_DoesNotMatchAcrossPrefixes()
        {
            var translations = new Dictionary<string, string> { ["SpId:88"] = "足球" };

            Assert.Equal("Premier League", MongoTranslationResolver.ResolveName(translations, 88, "LeId", "Premier League"));
        }

        #endregion

        #region ComposeFixtureName

        [Fact]
        public void ComposeFixtureName_JoinsBothTranslatedTeams()
        {
            var home = Teams(("ZH", "弗盧米嫩塞"));
            var away = Teams(("ZH", "佛朗明哥"));

            Assert.Equal("弗盧米嫩塞 vs. 佛朗明哥", MongoTranslationResolver.ComposeFixtureName(home, away, "ZH"));
        }

        // Without per-side English names there is nothing to splice a half-translated fixture from,
        // so the caller gets null and keeps the whole English fixture name.
        [Fact]
        public void ComposeFixtureName_WithoutFallbacks_ReturnsNull_WhenEitherSideUntranslated()
        {
            var home = Teams(("ZH", "弗盧米嫩塞"));
            var away = Teams(("ES", "Flamengo"));

            Assert.Null(MongoTranslationResolver.ComposeFixtureName(home, away, "ZH"));
            Assert.Null(MongoTranslationResolver.ComposeFixtureName(away, home, "ZH"));
        }

        // With them, each side falls back on its own: one missing translation must not drag the
        // translated opponent back to English.
        [Fact]
        public void ComposeFixtureName_WithFallbacks_KeepsTranslatedSideAndFallsBackPerSide()
        {
            var home = Teams(("ZH", "弗盧米嫩塞"));
            var away = Teams(("ES", "Flamengo"));

            Assert.Equal("弗盧米嫩塞 vs. Flamengo", MongoTranslationResolver.ComposeFixtureName(home, away, "ZH", "Fluminense FC", "Flamengo"));
            Assert.Equal("Fluminense FC vs. 弗盧米嫩塞", MongoTranslationResolver.ComposeFixtureName(away, home, "ZH", "Fluminense FC", "Flamengo"));
        }

        [Fact]
        public void ComposeFixtureName_WithFallbacks_ComposesFromEnglishOnly_WhenNeitherSideTranslated()
        {
            Assert.Equal(
                "Fluminense FC vs. Flamengo",
                MongoTranslationResolver.ComposeFixtureName(null, null, "ZH", "Fluminense FC", "Flamengo"));
        }

        // A blank fallback would compose "vs. Flamengo", so the whole name is abandoned instead.
        [Fact]
        public void ComposeFixtureName_ReturnsNull_WhenAFallbackIsEmpty()
        {
            Assert.Null(MongoTranslationResolver.ComposeFixtureName(null, null, "ZH", "", "Flamengo"));
            Assert.Null(MongoTranslationResolver.ComposeFixtureName(null, null, "ZH", "Fluminense FC", null));
        }

        #endregion
    }
}
