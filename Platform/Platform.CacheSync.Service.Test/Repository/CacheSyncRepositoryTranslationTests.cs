using Microsoft.Extensions.Configuration;
using Platform.CacheSync.Service.Repository;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Platform.CacheSync.Service.Test.Repository
{
    // CacheSync sits between every translation producer (Backoffice glossary edits, the DeepL
    // updater via Platform.API) and both OddsEngine consumers. It purges the Sportsbook.API
    // translation cache and relays the deltas; if either half is wrong the platform serves stale
    // or English names with no error anywhere.
    //
    // "localhost" is used as the configured host so DeleteCachePreload_SportsbookApi's
    // Dns.GetHostAddressesAsync resolves without a network. It can yield both 127.0.0.1 and ::1,
    // so purge assertions look at the first recorded request rather than a fixed count.
    public class CacheSyncRepositoryTranslationTests
    {
        private static CacheSyncRepository CreateRepository(FakeSportsbookRepository sportsbook, FakeMessageBroker broker)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["API:PlatformSportsbookAPI:Host"] = "localhost",
                    ["API:PlatformSportsbookAPI:Port"] = "5000"
                })
                .Build();

            return new CacheSyncRepository(null, sportsbook, broker, configuration);
        }

        private static OnTranslationChangeMessage MessageWith(params TranslationChangeItem[] items)
        {
            return new OnTranslationChangeMessage { Tr = items.ToList() };
        }

        #region ProcessTranslationChange

        [Fact]
        public async Task ProcessTranslationChange_NullMessage_DoesNotPurgeOrRelay()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessTranslationChange(null);

            Assert.Empty(sportsbook.Requests);
            Assert.Empty(broker.Published);
        }

        [Fact]
        public async Task ProcessTranslationChange_EmptyDeltaList_DoesNotPurgeOrRelay()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessTranslationChange(MessageWith());

            Assert.Empty(sportsbook.Requests);
            Assert.Empty(broker.Published);
        }

        [Fact]
        public async Task ProcessTranslationChange_PurgesPerChangedKey_NotPurgeAll()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessTranslationChange(MessageWith(
                new TranslationChangeItem { StId = "MaTe:259", LaCo = "ZH", Tx = "大/小" },
                new TranslationChangeItem { StId = "Te:4471", LaCo = "ES", Tx = "Fluminense" }));

            var request = sportsbook.Requests.First();

            Assert.Equal(CacheType.DynamicTranslation, request.CacheType);
            Assert.False(request.PurgeAll);
            Assert.Equal(2, request.Translations.Count);

            Assert.Equal("MaTe:259", request.Translations[0].StringId);
            Assert.Equal("ZH", request.Translations[0].TargetLanguage);
            Assert.Equal("大/小", request.Translations[0].TargetText);

            Assert.Equal("Te:4471", request.Translations[1].StringId);
            Assert.Equal("ES", request.Translations[1].TargetLanguage);
            Assert.Equal("Fluminense", request.Translations[1].TargetText);
        }

        [Fact]
        public async Task ProcessTranslationChange_DropsDeltasWithoutKeyOrLanguage()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessTranslationChange(MessageWith(
                null,
                new TranslationChangeItem { StId = "", LaCo = "ZH", Tx = "x" },
                new TranslationChangeItem { StId = "   ", LaCo = "ZH", Tx = "x" },
                new TranslationChangeItem { StId = "Te:1", LaCo = "", Tx = "x" },
                new TranslationChangeItem { StId = "Te:2", LaCo = "ZH", Tx = "ok" }));

            var request = sportsbook.Requests.First();

            Assert.Single(request.Translations);
            Assert.Equal("Te:2", request.Translations[0].StringId);
        }

        // An empty Tx is the documented "clear this translation" signal, not a missing value —
        // filtering it out would leave a stale translation live in every downstream cache.
        [Fact]
        public async Task ProcessTranslationChange_KeepsDeltaWithEmptyTargetText()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessTranslationChange(MessageWith(
                new TranslationChangeItem { StId = "Te:2", LaCo = "ZH", Tx = "" }));

            var request = sportsbook.Requests.First();

            Assert.Single(request.Translations);
            Assert.Equal(string.Empty, request.Translations[0].TargetText);
        }

        [Fact]
        public async Task ProcessTranslationChange_RelaysOriginalMessageOnOddsEngineRoutingKey()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();
            var message = MessageWith(new TranslationChangeItem { StId = "Te:2", LaCo = "ZH", Tx = "羅傑斯" });

            await CreateRepository(sportsbook, broker).ProcessTranslationChange(message);

            var published = Assert.Single(broker.Published);

            Assert.Equal("OnTranslationChange", published.MessageType);
            Assert.Equal("r.oe.ontranslationchange", published.RoutingKey);
            Assert.False(string.IsNullOrWhiteSpace(published.CorrelationId));

            // The relay must forward the message itself. Re-projecting it here would drop any
            // field Platform.Shared's copy does not declare.
            Assert.Same(message, published.Payload);
        }

        [Fact]
        public async Task ProcessTranslationChange_PurgeFailed_DoesNotRelay()
        {
            var sportsbook = new FakeSportsbookRepository { StatusToReturn = StatusCode.Error };
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessTranslationChange(MessageWith(
                new TranslationChangeItem { StId = "Te:2", LaCo = "ZH", Tx = "羅傑斯" }));

            Assert.NotEmpty(sportsbook.Requests);
            Assert.Empty(broker.Published);
        }

        #endregion

        #region ProcessDynamicTranslationChange

        // The legacy purge-only path carries no keys, so it must set PurgeAll — without it
        // Sportsbook.API's empty-keys guard silently no-ops and nothing is purged.
        [Fact]
        public async Task ProcessDynamicTranslationChange_PurgesEverything()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessDynamicTranslationChange(new OnDeleteCacheMessage());

            var request = sportsbook.Requests.First();

            Assert.Equal(CacheType.DynamicTranslation, request.CacheType);
            Assert.True(request.PurgeAll);
        }

        [Fact]
        public async Task ProcessDynamicTranslationChange_FansOutToTenAdapterInstances()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessDynamicTranslationChange(new OnDeleteCacheMessage());

            Assert.Equal(10, broker.Published.Count);
            Assert.All(broker.Published, p => Assert.Equal("OnDynamicTranslationChange", p.MessageType));

            var expected = Enumerable.Range(0, 10).Select(i => $"r.oe.adapter.ondynamictranslationchange.{i:D2}");

            Assert.Equal(expected, broker.Published.Select(p => p.RoutingKey));
        }

        [Fact]
        public async Task ProcessDynamicTranslationChange_PurgeFailed_DoesNotFanOut()
        {
            var sportsbook = new FakeSportsbookRepository { StatusToReturn = StatusCode.Error };
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessDynamicTranslationChange(new OnDeleteCacheMessage());

            Assert.Empty(broker.Published);
        }

        #endregion

        #region ProcessStaticTranslationChange

        [Fact]
        public async Task ProcessStaticTranslationChange_PurgesStaticCacheWithTheEditedKey()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessStaticTranslationChange(new OnDeleteCacheMessage { CacheKey = "tr-zh" });

            var request = sportsbook.Requests.First();

            Assert.Equal(CacheType.StaticTranslation, request.CacheType);
            Assert.Equal("tr-zh", request.CacheKey);
        }

        [Fact]
        public async Task ProcessStaticTranslationChange_NullMessage_PurgesWithEmptyKey()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessStaticTranslationChange(null);

            Assert.Equal(string.Empty, sportsbook.Requests.First().CacheKey);
        }

        // Nothing downstream of Sportsbook.API consumes static labels, so this path must stay
        // purge-only — a relay here would wake every adapter for a UI label edit.
        [Fact]
        public async Task ProcessStaticTranslationChange_NeverRelays()
        {
            var sportsbook = new FakeSportsbookRepository();
            var broker = new FakeMessageBroker();

            await CreateRepository(sportsbook, broker).ProcessStaticTranslationChange(new OnDeleteCacheMessage { CacheKey = "tr" });

            Assert.Empty(broker.Published);
        }

        #endregion
    }
}
