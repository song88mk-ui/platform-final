using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Sportsbook.API.Utility;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // GetSports (the sports tree) builds its response from these fields only: a field dropped here
    // silently turns into a default value (0, null) in the tree, and a field added here is transferred
    // and deserialized for every fixture on every cache miss.
    public class SportsTreeProjectionTests
    {
        private static readonly string[] Fields =
            { "_id", "FiId", "ClCo", "SpId", "SpNa", "SegId", "SegNa", "LeId", "LeNa", "FiStDa", "FiStId", "FiTyId" };

        private static BsonDocument Render(LanguageCode languageCode) =>
            SportsTreeProjection.For(languageCode).Render(
                BsonSerializer.SerializerRegistry.GetSerializer<Fixture>(),
                BsonSerializer.SerializerRegistry);

        [Fact]
        public void English_IncludesOnlyTheFieldsTheTreeUses()
        {
            var rendered = Render(LanguageCode.EN);

            Assert.Equal(Fields.OrderBy(f => f), rendered.Names.OrderBy(n => n));
            Assert.All(rendered.Values, v => Assert.Equal(1, v.ToInt32()));
        }

        [Fact]
        public void OtherLanguages_AddSportSegmentAndLeagueTranslations_ForThatLanguageOnly()
        {
            var rendered = Render(LanguageCode.ZH);

            Assert.Equal(Fields.Concat(new[] { "SpNaTr", "SegNaTr", "LeNaTr" }).OrderBy(f => f), rendered.Names.OrderBy(n => n));

            // The *Tr items map LaCo to _id ([BsonId]), so the element filter has to name _id.
            var languageOnly = new BsonDocument("$elemMatch", new BsonDocument("_id", "ZH"));
            Assert.Equal<BsonValue>(languageOnly, rendered["SpNaTr"]);
            Assert.Equal<BsonValue>(languageOnly, rendered["SegNaTr"]);
            Assert.Equal<BsonValue>(languageOnly, rendered["LeNaTr"]);
        }

        [Fact]
        public void TeamNameTranslations_AreNeverRead()
        {
            foreach (var languageCode in new[] { LanguageCode.EN, LanguageCode.ZH })
            {
                var rendered = Render(languageCode);

                Assert.False(rendered.Contains("HoNaTr"));
                Assert.False(rendered.Contains("AwNaTr"));
            }
        }

        [Fact]
        public void ProjectedDocument_DeserializesWithTheTranslations()
        {
            // What Mongo returns for a ZH request: the projected fields and one translation each.
            var projected = new BsonDocument
            {
                { "_id", "f1" },
                { "FiId", 10L },
                { "ClCo", "ABC" },
                { "SpId", 1 },
                { "SpNa", "Soccer" },
                { "SegId", 2 },
                { "SegNa", "England" },
                { "LeId", 3 },
                { "LeNa", "Premier League" },
                { "FiStDa", new BsonDateTime(new System.DateTime(2026, 10, 30, 12, 0, 0, System.DateTimeKind.Utc)) },
                { "FiStId", 0 },
                { "FiTyId", 1 },
                { "SpNaTr", new BsonArray { new BsonDocument { { "_id", "ZH" }, { "SpNa", "足球" } } } },
                { "SegNaTr", new BsonArray { new BsonDocument { { "_id", "ZH" }, { "SegNa", "英格兰" } } } },
                { "LeNaTr", new BsonArray { new BsonDocument { { "_id", "ZH" }, { "LeNa", "英超" } } } },
            };

            var fixture = BsonSerializer.Deserialize<SportsTreeFixtureProjection>(projected);

            Assert.Equal(10L, fixture.FiId);
            Assert.Equal("足球", MongoTranslationResolver.FindTranslation(fixture.SpNaTr, "ZH", t => t.LaCo, t => t.SpNa));
            Assert.Equal("英格兰", MongoTranslationResolver.FindTranslation(fixture.SegNaTr, "ZH", t => t.LaCo, t => t.SegNa));
            Assert.Equal("英超", MongoTranslationResolver.FindTranslation(fixture.LeNaTr, "ZH", t => t.LaCo, t => t.LeNa));
            Assert.Null(fixture.HoNaTr);
            Assert.Null(fixture.AwNaTr);
        }
    }
}
