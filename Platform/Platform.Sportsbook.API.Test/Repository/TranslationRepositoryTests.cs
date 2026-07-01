using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Shared.Data.Sportsbook;
using Platform.Shared.Data.Translation.SQLEntities;
using Platform.Shared.Utility;
using Platform.Sportsbook.API.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Platform.Sportsbook.API.Test.Repository
{
    // Covers TranslationRepository's two resolution paths: GetTranslationsByContentAsync (default,
    // resolves by TranslationCache.SOURCE_HASH -- a hash of the entity's source text) and
    // GetTranslationCacheAsync (opt-in template-key path, resolves by TranslationCache.STRING_ID --
    // the same key bet-genius writes to SportsbookGlossary, e.g. "MaTe:100", "Te:55").
    public class TranslationRepositoryTests
    {
        private static TranslationContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TranslationContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TranslationContext(options);
        }

        private static TranslationRepository CreateRepository(TranslationContext context, FakeMemCache memCache)
            => new TranslationRepository(context, memCache, NullLogger<TranslationRepository>.Instance);

        [Fact]
        public async Task GetTranslationsByContentAsync_EnglishLanguage_ReturnsEmptyWithoutLookup()
        {
            using var context = CreateContext();
            var repository = CreateRepository(context, new FakeMemCache());

            var result = await repository.GetTranslationsByContentAsync(
                new Dictionary<string, string> { ["MaId:1"] = "Money Line" },
                "EN");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTranslationsByContentAsync_ResolvesBySourceHash()
        {
            using var context = CreateContext();
            context.TranslationCache.Add(new TranslationCache
            {
                SourceHash = TranslationHashing.SourceHash("Money Line"),
                StringId = TranslationHashing.SourceHash("Money Line"),
                SourceLanguage = "EN",
                TargetLanguage = "ZH",
                SourceText = "Money Line",
                TargetText = "让球"
            });
            await context.SaveChangesAsync();

            var repository = CreateRepository(context, new FakeMemCache());

            var result = await repository.GetTranslationsByContentAsync(
                new Dictionary<string, string> { ["MaId:100"] = "Money Line" },
                "ZH");

            Assert.Equal("让球", result["MaId:100"]);
        }

        [Fact]
        public async Task GetTranslationsByContentAsync_MissingTargetText_IsExcludedFromResult()
        {
            using var context = CreateContext();
            context.TranslationCache.Add(new TranslationCache
            {
                SourceHash = TranslationHashing.SourceHash("Total Goals"),
                StringId = TranslationHashing.SourceHash("Total Goals"),
                SourceLanguage = "EN",
                TargetLanguage = "ZH",
                SourceText = "Total Goals",
                TargetText = null
            });
            await context.SaveChangesAsync();

            var repository = CreateRepository(context, new FakeMemCache());

            var result = await repository.GetTranslationsByContentAsync(
                new Dictionary<string, string> { ["MaId:200"] = "Total Goals" },
                "ZH");

            Assert.False(result.ContainsKey("MaId:200"));
        }

        [Fact]
        public async Task GetTranslationsByContentAsync_TwoCallerKeysSharingOneSourceText_BothResolve()
        {
            // Two different markets with identical names share one TranslationCache row.
            using var context = CreateContext();
            context.TranslationCache.Add(new TranslationCache
            {
                SourceHash = TranslationHashing.SourceHash("Match Winner"),
                StringId = TranslationHashing.SourceHash("Match Winner"),
                SourceLanguage = "EN",
                TargetLanguage = "ZH",
                SourceText = "Match Winner",
                TargetText = "获胜方"
            });
            await context.SaveChangesAsync();

            var repository = CreateRepository(context, new FakeMemCache());

            var result = await repository.GetTranslationsByContentAsync(
                new Dictionary<string, string>
                {
                    ["MaId:1"] = "Match Winner",
                    ["MaId:2"] = "Match Winner"
                },
                "ZH");

            Assert.Equal("获胜方", result["MaId:1"]);
            Assert.Equal("获胜方", result["MaId:2"]);
        }

        [Fact]
        public async Task GetTranslationsByContentAsync_SecondCall_ResolvesFromCacheWithoutTheRow()
        {
            using var context = CreateContext();
            context.TranslationCache.Add(new TranslationCache
            {
                SourceHash = TranslationHashing.SourceHash("Correct Score"),
                StringId = TranslationHashing.SourceHash("Correct Score"),
                SourceLanguage = "EN",
                TargetLanguage = "ZH",
                SourceText = "Correct Score",
                TargetText = "正确比分"
            });
            await context.SaveChangesAsync();

            var memCache = new FakeMemCache();
            var repository = CreateRepository(context, memCache);
            var keyToSourceText = new Dictionary<string, string> { ["MaId:3"] = "Correct Score" };

            var first = await repository.GetTranslationsByContentAsync(keyToSourceText, "ZH");
            Assert.Equal("正确比分", first["MaId:3"]);

            // Row removed -- a second call must still resolve from the mem-cache entry populated above.
            context.TranslationCache.RemoveRange(context.TranslationCache);
            await context.SaveChangesAsync();

            var second = await repository.GetTranslationsByContentAsync(keyToSourceText, "ZH");
            Assert.Equal("正确比分", second["MaId:3"]);
        }

        [Fact]
        public async Task GetTranslationsByContentAsync_WrongSourceLanguage_IsNotMatched()
        {
            // TARGET_LANGUAGE matches but SOURCE_LANGUAGE doesn't -- must not be returned as an EN-source translation.
            using var context = CreateContext();
            context.TranslationCache.Add(new TranslationCache
            {
                SourceHash = TranslationHashing.SourceHash("Draw No Bet"),
                StringId = TranslationHashing.SourceHash("Draw No Bet"),
                SourceLanguage = "TH",
                TargetLanguage = "ZH",
                SourceText = "Draw No Bet",
                TargetText = "不平局"
            });
            await context.SaveChangesAsync();

            var repository = CreateRepository(context, new FakeMemCache());

            var result = await repository.GetTranslationsByContentAsync(
                new Dictionary<string, string> { ["MaId:4"] = "Draw No Bet" },
                "ZH");

            Assert.False(result.ContainsKey("MaId:4"));
        }

        [Fact]
        public async Task GetTranslationCacheAsync_EnglishLanguage_ReturnsEmptyWithoutLookup()
        {
            using var context = CreateContext();
            var repository = CreateRepository(context, new FakeMemCache());

            var result = await repository.GetTranslationCacheAsync(
                new Dictionary<string, string> { ["MaId:1"] = "MaTe:100" },
                "EN");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTranslationCacheAsync_ResolvesByStringId()
        {
            using var context = CreateContext();
            context.TranslationCache.Add(new TranslationCache
            {
                SourceHash = "HASH-1",
                StringId = "MaTe:100",
                SourceLanguage = "EN",
                TargetLanguage = "ZH",
                SourceText = "Match Result",
                TargetText = "比赛结果"
            });
            await context.SaveChangesAsync();

            var repository = CreateRepository(context, new FakeMemCache());

            var result = await repository.GetTranslationCacheAsync(
                new Dictionary<string, string> { ["MaId:1"] = "MaTe:100" },
                "ZH");

            Assert.Equal("比赛结果", result["MaId:1"]);
        }

        [Fact]
        public async Task GetTranslationCacheAsync_MissingTargetText_IsExcludedFromResult()
        {
            using var context = CreateContext();
            context.TranslationCache.Add(new TranslationCache
            {
                SourceHash = "HASH-2",
                StringId = "MaTe:200",
                SourceLanguage = "EN",
                TargetLanguage = "ZH",
                SourceText = "Total Goals",
                TargetText = null
            });
            await context.SaveChangesAsync();

            var repository = CreateRepository(context, new FakeMemCache());

            var result = await repository.GetTranslationCacheAsync(
                new Dictionary<string, string> { ["MaId:2"] = "MaTe:200" },
                "ZH");

            Assert.False(result.ContainsKey("MaId:2"));
        }

        [Fact]
        public async Task GetTranslationCacheAsync_TwoCallerKeysSharingOneStringId_BothResolve()
        {
            // Home and away team names for different fixtures can point at the same Te:{teamId} row.
            using var context = CreateContext();
            context.TranslationCache.Add(new TranslationCache
            {
                SourceHash = "HASH-3",
                StringId = "Te:55",
                SourceLanguage = "EN",
                TargetLanguage = "ZH",
                SourceText = "Tigers",
                TargetText = "老虎队"
            });
            await context.SaveChangesAsync();

            var repository = CreateRepository(context, new FakeMemCache());

            var result = await repository.GetTranslationCacheAsync(
                new Dictionary<string, string>
                {
                    ["Ho:1001"] = "Te:55",
                    ["Aw:1002"] = "Te:55"
                },
                "ZH");

            Assert.Equal("老虎队", result["Ho:1001"]);
            Assert.Equal("老虎队", result["Aw:1002"]);
        }

        [Fact]
        public async Task GetTranslationCacheAsync_SecondCall_ResolvesFromCacheWithoutTheRow()
        {
            using var context = CreateContext();
            context.TranslationCache.Add(new TranslationCache
            {
                SourceHash = "HASH-4",
                StringId = "MaTe:300",
                SourceLanguage = "EN",
                TargetLanguage = "ZH",
                SourceText = "Correct Score",
                TargetText = "正确比分"
            });
            await context.SaveChangesAsync();

            var memCache = new FakeMemCache();
            var repository = CreateRepository(context, memCache);
            var keyToStringId = new Dictionary<string, string> { ["MaId:3"] = "MaTe:300" };

            var first = await repository.GetTranslationCacheAsync(keyToStringId, "ZH");
            Assert.Equal("正确比分", first["MaId:3"]);

            // Row removed — a second call must still resolve from the mem-cache entry populated above.
            context.TranslationCache.Remove(context.TranslationCache.Single());
            await context.SaveChangesAsync();

            var second = await repository.GetTranslationCacheAsync(keyToStringId, "ZH");
            Assert.Equal("正确比分", second["MaId:3"]);
        }
    }
}
