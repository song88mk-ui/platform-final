using System;
using System.Collections.Generic;
using Platform.Shared.Data.Sportsbook.SQLEntities;
using Platform.Sportsbook.API.Test.Repository;
using Platform.Sportsbook.API.Utility;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // BO-152. Segment and league order are cached per client, and the OnSportSortOrderChange purge
    // used exact deletes of "seo" and "leo", so a saved order stayed stale until the pod restarted.
    public class SportSortOrderCacheTests
    {
        private static readonly Guid ClientA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ClientB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        private static FakeMemCache CacheFor(params Guid[] clients)
        {
            var cache = new FakeMemCache();
            cache.AddOrUpdateCache(SportSortOrderCache.SportOrderKey, new List<ClientSportOrder>(), 24);

            foreach (var client in clients)
            {
                cache.AddOrUpdateCache(SportSortOrderCache.SegmentOrderKey(client), new List<ClientSegmentOrder>(), 24);
                cache.AddOrUpdateCache(SportSortOrderCache.LeagueOrderKey(client), new List<ClientLeagueOrder>(), 24);
            }

            return cache;
        }

        [Fact]
        public void PurgeCache_RemovesTheSportOrder()
        {
            var cache = CacheFor(ClientA);

            SportSortOrderCache.PurgeCache(cache);

            Assert.Null(cache.GetCache<List<ClientSportOrder>>(SportSortOrderCache.SportOrderKey));
        }

        [Fact]
        public void PurgeCache_RemovesEveryClientsSegmentOrder()
        {
            var cache = CacheFor(ClientA, ClientB);

            SportSortOrderCache.PurgeCache(cache);

            Assert.Null(cache.GetCache<List<ClientSegmentOrder>>(SportSortOrderCache.SegmentOrderKey(ClientA)));
            Assert.Null(cache.GetCache<List<ClientSegmentOrder>>(SportSortOrderCache.SegmentOrderKey(ClientB)));
        }

        [Fact]
        public void PurgeCache_RemovesEveryClientsLeagueOrder()
        {
            var cache = CacheFor(ClientA, ClientB);

            SportSortOrderCache.PurgeCache(cache);

            Assert.Null(cache.GetCache<List<ClientLeagueOrder>>(SportSortOrderCache.LeagueOrderKey(ClientA)));
            Assert.Null(cache.GetCache<List<ClientLeagueOrder>>(SportSortOrderCache.LeagueOrderKey(ClientB)));
        }

        [Fact]
        public void PurgeCache_LeavesTheOddstableLeagueOrderAlone()
        {
            // "oleo-" (BO-107) is purged on its own message; "leo-" must not reach into it.
            var cache = CacheFor(ClientA);
            cache.AddOrUpdateCache(OddstableLeagueSortOrder.CacheKey(ClientA), new List<ClientOddstableLeagueOrder>(), 24);

            SportSortOrderCache.PurgeCache(cache);

            Assert.NotNull(cache.GetCache<List<ClientOddstableLeagueOrder>>(OddstableLeagueSortOrder.CacheKey(ClientA)));
        }

        [Fact]
        public void Keys_AreLowerCaseAndPerClient()
        {
            var upper = Guid.Parse("ABCDEF00-0000-0000-0000-000000000001");

            Assert.Equal("seo-abcdef00-0000-0000-0000-000000000001", SportSortOrderCache.SegmentOrderKey(upper));
            Assert.Equal("leo-abcdef00-0000-0000-0000-000000000001", SportSortOrderCache.LeagueOrderKey(upper));
            Assert.NotEqual(SportSortOrderCache.SegmentOrderKey(ClientA), SportSortOrderCache.SegmentOrderKey(ClientB));
        }
    }
}
