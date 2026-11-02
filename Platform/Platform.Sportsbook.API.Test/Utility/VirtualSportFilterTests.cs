using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Extensions;
using Platform.Sportsbook.API.Utility;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // The driver builds the Mongo query when the find runs, so a predicate it cannot translate only
    // fails at runtime, on every request. Rendering the filters here catches that in CI.
    public class VirtualSportFilterTests
    {
        private static BsonDocument Render(FilterDefinition<Fixture> filter) =>
            filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<Fixture>(), BsonSerializer.SerializerRegistry);

        [Fact]
        public void ActiveMatchesExcludingVirtual_RendersAsAMongoFilter()
        {
            var rendered = Render(Builders<Fixture>.Filter.Where(VirtualSportFilter.ActiveMatchesExcludingVirtual()));

            Assert.Equal(BsonDocument.Parse("{ FiStId: { $lt: 2 }, FiTyId: 0, SpId: { $nin: [] } }"), rendered);
        }

        [Fact]
        public void VirtualSportFixtures_RendersAsAMongoFilter()
        {
            var rendered = Render(Builders<Fixture>.Filter.Where(VirtualSportFilter.VirtualSportFixtures()));

            Assert.Equal(BsonDocument.Parse("{ FiStId: { $lt: 12 }, SpId: { $in: [] } }"), rendered);
        }

        [Fact]
        public void TheExtensionMethodForm_CannotBeTranslated()
        {
            // The form these filters replace. If a driver upgrade starts translating it, this test
            // fails and the comment in VirtualSportFilter can be revisited.
            var filter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && !c.SpId.IsVirtualSportId());

            Assert.Throws<System.ArgumentException>(() => Render(filter));
        }
    }
}
