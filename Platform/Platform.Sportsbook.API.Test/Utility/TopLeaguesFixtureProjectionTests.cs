using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Sportsbook.API.Utility;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // GetTopLeagues builds its response from these fields only: a field dropped here silently turns
    // into a default value (0, null) in the response, and a field added here is transferred and
    // deserialized for every fixture of the sport on every cache miss.
    public class TopLeaguesFixtureProjectionTests
    {
        private static readonly string[] Fields = { "SpId", "SegId", "SegNa", "LeId", "LeNa", "FiStId", "ClCo" };

        private static BsonDocument Render(LanguageCode languageCode) =>
            TopLeaguesFixtureProjection.For(languageCode).Render(
                BsonSerializer.SerializerRegistry.GetSerializer<Fixture>(),
                BsonSerializer.SerializerRegistry);

        [Fact]
        public void English_IncludesOnlyTheFieldsTheResponseUses()
        {
            var rendered = Render(LanguageCode.EN);

            Assert.Equal(Fields.OrderBy(f => f), rendered.Names.OrderBy(n => n));
            Assert.All(rendered.Values, v => Assert.Equal(1, v.ToInt32()));
        }

        [Fact]
        public void OtherLanguages_AddSegmentAndLeagueTranslations_ForThatLanguageOnly()
        {
            var rendered = Render(LanguageCode.ZH);

            Assert.Equal(Fields.Concat(new[] { "SegNaTr", "LeNaTr" }).OrderBy(f => f), rendered.Names.OrderBy(n => n));

            // The *Tr items map LaCo to _id ([BsonId]), so the element filter has to name _id.
            var languageOnly = new BsonDocument("$elemMatch", new BsonDocument("_id", "ZH"));
            Assert.Equal<BsonValue>(languageOnly, rendered["SegNaTr"]);
            Assert.Equal<BsonValue>(languageOnly, rendered["LeNaTr"]);
        }

        [Fact]
        public void ProjectedDocument_DeserializesWithTheTranslationAndWithoutTheRest()
        {
            // What Mongo returns for a ZH request: the projected fields, _id, and one translation each.
            var projected = new BsonDocument
            {
                { "_id", "f1" },
                { "SpId", 1 },
                { "SegId", 2 },
                { "SegNa", "England" },
                { "LeId", 3 },
                { "LeNa", "Premier League" },
                { "FiStId", 1 },
                { "ClCo", "ABC" },
                { "SegNaTr", new BsonArray { new BsonDocument { { "_id", "ZH" }, { "SegNa", "英格兰" } } } },
                { "LeNaTr", new BsonArray { new BsonDocument { { "_id", "ZH" }, { "LeNa", "英超" } } } },
            };

            var fixture = BsonSerializer.Deserialize<Fixture>(projected);

            Assert.Equal("英格兰", MongoTranslationResolver.FindTranslation(fixture.SegNaTr, "ZH", t => t.LaCo, t => t.SegNa));
            Assert.Equal("英超", MongoTranslationResolver.FindTranslation(fixture.LeNaTr, "ZH", t => t.LaCo, t => t.LeNa));
            Assert.Null(fixture.FiNaTr);
            Assert.Null(fixture.HoNaTr);
            Assert.Null(fixture.PeSc);
        }
    }
}
