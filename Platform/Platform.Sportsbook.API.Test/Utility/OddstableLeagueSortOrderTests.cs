using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Shared.Data.Sportsbook.SQLEntities;
using Platform.Sportsbook.API.Test.Repository;
using Platform.Sportsbook.API.Utility;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // GM-851. Back Office (BO-107) stores a per-client Oddstable league order next to the Sport Tree
    // order, and the Early, Today and Live lists have to follow it. The two failure modes that matter
    // are a client with no Oddstable configuration changing at all, and a league the configuration
    // leaves out landing in front of the configured ones.
    public class OddstableLeagueSortOrderTests
    {
        private const int Football = 1;
        private const int Tennis = 2;

        private static ClientLeagueOrder Tree(int sportId, int segmentId, int leagueId, int sortOrder) =>
            new ClientLeagueOrder { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId, SortOrder = sortOrder };

        private static ClientOddstableLeagueOrder Oddstable(int sportId, int segmentId, int leagueId, int sortOrder) =>
            new ClientOddstableLeagueOrder { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId, SortOrder = sortOrder };

        private static OddstableLeagueSortOrder Build(
            IEnumerable<ClientLeagueOrder> tree = null,
            IEnumerable<ClientOddstableLeagueOrder> oddstable = null) =>
            OddstableLeagueSortOrder.Build(tree, oddstable);

        #region Of, no Oddstable configuration

        [Fact]
        public void Of_NoOddstableRows_UsesTheSportTreeOrder()
        {
            var order = Build(new[] { Tree(Football, 10, 100, 7), Tree(Football, 10, 101, 3) });

            Assert.Equal(7, order.Of(Football, 10, 100));
            Assert.Equal(3, order.Of(Football, 10, 101));
        }

        [Fact]
        public void Of_NoOddstableRows_LeagueWithoutASportTreeRow_GetsTheDefault()
        {
            var order = Build(new[] { Tree(Football, 10, 100, 7) });

            Assert.Equal(OddstableLeagueSortOrder.DefaultOrder, order.Of(Football, 10, 999));
        }

        [Fact]
        public void Of_NoOddstableRows_SportTreeOrderOfZero_GetsTheDefault()
        {
            // 0 means "not set" in ClientLeagueOrder, as it did before the Oddstable had its own order.
            var order = Build(new[] { Tree(Football, 10, 100, 0) });

            Assert.Equal(OddstableLeagueSortOrder.DefaultOrder, order.Of(Football, 10, 100));
        }

        [Fact]
        public void Of_NullInputs_GetTheDefault()
        {
            var order = Build(null, null);

            Assert.Equal(OddstableLeagueSortOrder.DefaultOrder, order.Of(Football, 10, 100));
        }

        #endregion

        #region Of, with Oddstable configuration

        [Fact]
        public void Of_ConfiguredLeague_UsesTheConfiguredOrder_NotTheSportTreeOrder()
        {
            var order = Build(
                new[] { Tree(Football, 10, 100, 1), Tree(Football, 10, 101, 2) },
                new[] { Oddstable(Football, 10, 101, 1), Oddstable(Football, 10, 100, 2) });

            Assert.Equal(1, order.Of(Football, 10, 101));
            Assert.Equal(2, order.Of(Football, 10, 100));
        }

        [Fact]
        public void Of_ConfiguredLeagueWithoutASportTreeRow_StillUsesTheConfiguredOrder()
        {
            var order = Build(null, new[] { Oddstable(Football, 10, 100, 4) });

            Assert.Equal(4, order.Of(Football, 10, 100));
        }

        [Fact]
        public void Of_LeagueTheConfigurationLeavesOut_SortsAfterEveryConfiguredLeague()
        {
            // The Sport Tree numbers (1 and 2) are smaller than the configured ones (up to 50), which is
            // the case where a plain lookup would put the unconfigured leagues in front.
            var configured = Enumerable.Range(1, 50).Select(i => Oddstable(Football, 10, 200 + i, i)).ToArray();
            var order = Build(new[] { Tree(Football, 10, 100, 1), Tree(Football, 10, 101, 2) }, configured);

            var lastConfigured = configured.Max(c => order.Of(Football, 10, c.LeagueId));

            Assert.True(order.Of(Football, 10, 100) > lastConfigured);
            Assert.True(order.Of(Football, 10, 101) > lastConfigured);
        }

        [Fact]
        public void Of_LeaguesTheConfigurationLeavesOut_KeepTheirSportTreeOrder()
        {
            var order = Build(
                new[] { Tree(Football, 10, 100, 5), Tree(Football, 10, 101, 2), Tree(Football, 10, 102, 9) },
                new[] { Oddstable(Football, 10, 200, 1), Oddstable(Football, 10, 201, 2) });

            var ranked = new[] { 100, 101, 102 }.OrderBy(l => order.Of(Football, 10, l)).ToArray();

            Assert.Equal(new[] { 101, 100, 102 }, ranked);
        }

        [Fact]
        public void Of_LeagueTheConfigurationLeavesOut_WithoutASportTreeRow_SortsAfterOnesThatHaveOne()
        {
            var order = Build(
                new[] { Tree(Football, 10, 100, 980) },
                new[] { Oddstable(Football, 10, 200, 1) });

            Assert.True(order.Of(Football, 10, 999) > order.Of(Football, 10, 100));
        }

        [Fact]
        public void Of_LeagueTheConfigurationLeavesOut_SportTreeOrderOfZero_IsTreatedAsNotSet()
        {
            var order = Build(
                new[] { Tree(Football, 10, 100, 0) },
                new[] { Oddstable(Football, 10, 200, 3) });

            Assert.Equal(3 + OddstableLeagueSortOrder.DefaultOrder, order.Of(Football, 10, 100));
        }

        [Fact]
        public void Of_ZeroBasedConfiguration_ConfiguredLeagueStillComesFirst()
        {
            var order = Build(
                new[] { Tree(Football, 10, 100, 1) },
                new[] { Oddstable(Football, 10, 200, 0), Oddstable(Football, 10, 201, 1) });

            Assert.True(order.Of(Football, 10, 201) < order.Of(Football, 10, 100));
        }

        [Fact]
        public void Of_ConfigurationForOneSport_DoesNotChangeAnotherSport()
        {
            var order = Build(
                new[] { Tree(Football, 10, 100, 7), Tree(Tennis, 20, 300, 4) },
                new[] { Oddstable(Football, 10, 200, 1) });

            Assert.Equal(4, order.Of(Tennis, 20, 300));
            Assert.Equal(OddstableLeagueSortOrder.DefaultOrder, order.Of(Tennis, 20, 999));
        }

        [Fact]
        public void Of_SameLeagueIdInAnotherSegment_IsAnotherLeague()
        {
            var order = Build(null, new[] { Oddstable(Football, 10, 100, 1), Oddstable(Football, 11, 100, 2) });

            Assert.Equal(1, order.Of(Football, 10, 100));
            Assert.Equal(2, order.Of(Football, 11, 100));
        }

        #endregion

        #region PurgeCache

        private static readonly Guid ClientA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClientB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private static readonly string[] ResponsePrefixes = { "nlf", "nlfbl", "ntf", "ntfbl", "nuf", "nufbl", "ndf", "ndfbl" };

        private static FakeMemCache CacheFor(params Guid[] clients)
        {
            var cache = new FakeMemCache();

            foreach (var client in clients)
            {
                cache.AddOrUpdateCache(OddstableLeagueSortOrder.CacheKey(client), new List<ClientOddstableLeagueOrder>(), 24);

                foreach (var prefix in ResponsePrefixes)
                    cache.AddOrUpdateCache($"{prefix}:{client.ToString().ToUpper()}:EN:1", "response", 24);
            }

            return cache;
        }

        private static bool Has(FakeMemCache cache, string key) => cache.GetCache<string>(key) != null
            || cache.GetCache<List<ClientOddstableLeagueOrder>>(key) != null;

        [Fact]
        public void PurgeCache_WithAClientId_RemovesThatClientsRowsAndEveryListResponse()
        {
            var cache = CacheFor(ClientA);

            OddstableLeagueSortOrder.PurgeCache(cache, ClientA.ToString());

            Assert.False(Has(cache, OddstableLeagueSortOrder.CacheKey(ClientA)));

            foreach (var prefix in ResponsePrefixes)
                Assert.False(Has(cache, $"{prefix}:{ClientA.ToString().ToUpper()}:EN:1"), prefix);
        }

        [Fact]
        public void PurgeCache_WithAClientId_LeavesOtherClientsAlone()
        {
            var cache = CacheFor(ClientA, ClientB);

            OddstableLeagueSortOrder.PurgeCache(cache, ClientA.ToString());

            Assert.True(Has(cache, OddstableLeagueSortOrder.CacheKey(ClientB)));

            foreach (var prefix in ResponsePrefixes)
                Assert.True(Has(cache, $"{prefix}:{ClientB.ToString().ToUpper()}:EN:1"), prefix);
        }

        [Fact]
        public void PurgeCache_WithAClientId_LeavesResponsesThatDoNotCarryTheLeagueOrderAlone()
        {
            var cache = CacheFor(ClientA);
            var upper = ClientA.ToString().ToUpper();

            // Sport counts and the sports tree share the prefix letters but never carry the league order.
            foreach (var key in new[] { $"nlfc:{upper}:EN:1", $"ntfc:{upper}:EN:1", $"ns:{upper}:EN", $"leo-{ClientA.ToString().ToLower()}" })
                cache.AddOrUpdateCache(key, "other", 24);

            OddstableLeagueSortOrder.PurgeCache(cache, ClientA.ToString());

            foreach (var key in new[] { $"nlfc:{upper}:EN:1", $"ntfc:{upper}:EN:1", $"ns:{upper}:EN", $"leo-{ClientA.ToString().ToLower()}" })
                Assert.True(Has(cache, key), key);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        public void PurgeCache_WithoutAUsableClientId_RemovesEveryClient(string clientId)
        {
            var cache = CacheFor(ClientA, ClientB);
            cache.AddOrUpdateCache($"nlfc:{ClientA.ToString().ToUpper()}:EN:1", "other", 24);

            OddstableLeagueSortOrder.PurgeCache(cache, clientId);

            foreach (var client in new[] { ClientA, ClientB })
            {
                Assert.False(Has(cache, OddstableLeagueSortOrder.CacheKey(client)));

                foreach (var prefix in ResponsePrefixes)
                    Assert.False(Has(cache, $"{prefix}:{client.ToString().ToUpper()}:EN:1"), prefix);
            }

            Assert.True(Has(cache, $"nlfc:{ClientA.ToString().ToUpper()}:EN:1"));
        }

        [Fact]
        public void CacheKey_IsLowerCaseAndDoesNotStartWithTheSportTreeKeyPrefix()
        {
            var key = OddstableLeagueSortOrder.CacheKey(ClientB);

            Assert.Equal("oleo-22222222-2222-2222-2222-222222222222", key);
            Assert.False(key.StartsWith("leo-"));
        }

        #endregion
    }
}
