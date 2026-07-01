using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Shared.Data.Sportsbook;
using Platform.Shared.Data.Translation.SQLEntities;
using Platform.Shared.Utility;
using Platform.Sportsbook.API.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Platform.Sportsbook.API.Test.Repository
{
    // Covers TranslationRepository.GetTranslationsByContentAsync -- resolves non-English translations
    // from TranslationCache by SOURCE_HASH (= hash of the entity's source text), not by StringId.
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
    }
}
