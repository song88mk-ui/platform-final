using Microsoft.Extensions.Configuration;
using Platform.CacheSync.Service.Repository;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using RabbitMQ.Client.Framing;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Platform.CacheSync.Service.Test.Repository
{
    // GM-851. Back Office publishes OnOddstableLeagueSortOrderChange after every League Sorting save or
    // copy (BO-107). Before this handler existed CacheSync ignored the message, so the Sportsbook API
    // kept serving the old order from its 24 hour caches. It also relays the change to the websocket
    // service, which caches the order it stamps on pushed fixtures.
    //
    // "localhost" is used as the configured host so DeleteCachePreload_SportsbookApi's
    // Dns.GetHostAddressesAsync resolves without a network. It can yield both 127.0.0.1 and ::1, so
    // assertions look at the first recorded request rather than a fixed count.
    public class CacheSyncRepositoryOddstableLeagueTests
    {
        private static readonly Guid ClientId = Guid.Parse("ed082939-0000-0000-0000-000000000001");

        private static IConfiguration Configuration() => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["API:PlatformSportsbookAPI:Host"] = "localhost",
                ["API:PlatformSportsbookAPI:Port"] = "5000"
            })
            .Build();

        private static CacheSyncRepository CreateRepository(FakeSportsbookRepository sportsbook, FakeMessageBroker broker) =>
            new CacheSyncRepository(null, sportsbook, broker, Configuration());

        // Serialised exactly as Backoffice.API does it (System.Text.Json over OnDeleteCacheMessage).
        private static string BackOfficePayload(Guid clientId) => JsonSerializer.Serialize(new OnDeleteCacheMessage
        {
            CacheType = CacheType.OddstableLeagueSortOrder,
            ClientId = clientId,
            ClientCode = "QTE-A",
            CacheKey = string.Empty
        });

        [Fact]
        public async Task ProcessOddstableLeagueSortOrderChange_PurgesTheSportsbookApiForThatClient()
        {
            var sportsbook = new FakeSportsbookRepository();

            await CreateRepository(sportsbook, new FakeMessageBroker())
                .ProcessOddstableLeagueSortOrderChange(new OnDeleteCacheMessage { ClientId = ClientId, ClientCode = "QTE-A" });

            Assert.NotEmpty(sportsbook.Requests);
            Assert.Equal(CacheType.OddstableLeagueSortOrder, sportsbook.Requests[0].CacheType);
            Assert.Equal(ClientId.ToString(), sportsbook.Requests[0].CacheKey);
            Assert.False(sportsbook.Requests[0].PurgeAll);
        }

        [Fact]
        public async Task ProcessOddstableLeagueSortOrderChange_WithoutAClientId_SendsAnEmptyKeySoEveryClientIsPurged()
        {
            var sportsbook = new FakeSportsbookRepository();

            await CreateRepository(sportsbook, new FakeMessageBroker())
                .ProcessOddstableLeagueSortOrderChange(new OnDeleteCacheMessage());

            Assert.NotEmpty(sportsbook.Requests);
            Assert.Equal(CacheType.OddstableLeagueSortOrder, sportsbook.Requests[0].CacheType);
            Assert.Equal(string.Empty, sportsbook.Requests[0].CacheKey);
        }

        // The websocket service stamps the league order on every fixture it pushes and only reloads it
        // when this relay arrives. The routing key suffix is how it finds the client.
        [Fact]
        public async Task ProcessOddstableLeagueSortOrderChange_RelaysToTheWebsocketServiceForThatClient()
        {
            var broker = new FakeMessageBroker();

            await CreateRepository(new FakeSportsbookRepository(), broker)
                .ProcessOddstableLeagueSortOrderChange(new OnDeleteCacheMessage { ClientId = ClientId, ClientCode = "QTE-A" });

            var relayed = Assert.Single(broker.Published);
            Assert.Equal("OnOddstableLeagueSortOrderChange", relayed.MessageType);
            Assert.Equal("r.oe.sb.engine.onoddstableleaguesortorderchange.qte-a", relayed.RoutingKey);
            Assert.False(string.IsNullOrEmpty(relayed.CorrelationId));
        }

        [Fact]
        public async Task ProcessOddstableLeagueSortOrderChange_WhenThePurgeFails_DoesNotRelay()
        {
            // A reload before the Sportsbook API has dropped its cache would read the old order again.
            var broker = new FakeMessageBroker();
            var sportsbook = new FakeSportsbookRepository { StatusToReturn = StatusCode.Error };

            await CreateRepository(sportsbook, broker)
                .ProcessOddstableLeagueSortOrderChange(new OnDeleteCacheMessage { ClientId = ClientId, ClientCode = "QTE-A" });

            Assert.Empty(broker.Published);
        }

        [Fact]
        public async Task ProcessOddstableLeagueSortOrderChange_WithoutAClientCode_DoesNotRelay()
        {
            // The routing key ends in the client code; without one there is nobody to address.
            var broker = new FakeMessageBroker();

            await CreateRepository(new FakeSportsbookRepository(), broker)
                .ProcessOddstableLeagueSortOrderChange(new OnDeleteCacheMessage { ClientId = ClientId });

            Assert.Empty(broker.Published);
        }

        // The case label in Worker has to match the MessageType Back Office publishes, or the message
        // is silently ignored, which is exactly what happened before GM-851.
        [Fact]
        public void Worker_OddstableLeagueSortOrderChangeMessage_ReachesTheHandlerWithTheClient()
        {
            var sportsbook = new FakeSportsbookRepository();
            var worker = new Worker(new FakeMessageBroker(), CreateRepository(sportsbook, new FakeMessageBroker()));

            var args = new BasicDeliverEventArgs
            {
                Body = Encoding.UTF8.GetBytes(BackOfficePayload(ClientId)),
                BasicProperties = new BasicProperties { Type = "OnOddstableLeagueSortOrderChange" }
            };

            typeof(Worker)
                .GetMethod("MessageReceivedEvent", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(worker, new object[] { null, args });

            Assert.NotEmpty(sportsbook.Requests);
            Assert.Equal(CacheType.OddstableLeagueSortOrder, sportsbook.Requests[0].CacheType);
            Assert.Equal(ClientId.ToString(), sportsbook.Requests[0].CacheKey);
        }
    }
}
