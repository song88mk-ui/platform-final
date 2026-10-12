using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Shared.Data.Sportsbook.SQLEntities;
using Platform.Shared.Utility;

namespace Platform.Sportsbook.API.Utility
{
    // League order for the Oddstable, the Early, Today and Live fixture lists (GM-851). Back Office
    // configures it per client and sport in ClientOddstableLeagueOrder (BO-107), independently of the
    // Sport Tree order in ClientLeagueOrder, so a change to one never moves the other.
    //
    // A sport with no Oddstable rows behaves exactly as before: the Sport Tree order, and 1000 for a
    // league without a usable row. Once a sport has Oddstable rows, a configured league takes its
    // configured order and every other league follows them, in the Sport Tree order. That is the rule
    // BO-107 gives for a league the configuration does not mention.
    //
    // It lives here as a plain class so the ordering and the cache purge can be unit-tested without a
    // Mongo or SQL instance, as SportsTreeFilter and LocalDayRange are.
    public sealed class OddstableLeagueSortOrder
    {
        // What a league without a usable Sport Tree row got before the Oddstable had its own order.
        public const int DefaultOrder = 1000;

        // Response caches whose payload carries the league order: Live, Today, Upcoming and Daily,
        // each as the whole list and as the by-league variant (SportsbookRepository mKey prefixes).
        private static readonly string[] ResponseCachePrefixes =
        {
            "nlf", "nlfbl", "ntf", "ntfbl", "nuf", "nufbl", "ndf", "ndfbl"
        };

        private const string RowsCachePrefix = "oleo-";

        private readonly Dictionary<(int SportId, int SegmentId, int LeagueId), int> _sportTree;
        private readonly Dictionary<(int SportId, int SegmentId, int LeagueId), int> _configured;
        private readonly Dictionary<int, int> _lastConfiguredBySport;

        private OddstableLeagueSortOrder(
            Dictionary<(int SportId, int SegmentId, int LeagueId), int> sportTree,
            Dictionary<(int SportId, int SegmentId, int LeagueId), int> configured,
            Dictionary<int, int> lastConfiguredBySport)
        {
            _sportTree = sportTree;
            _configured = configured;
            _lastConfiguredBySport = lastConfiguredBySport;
        }

        public static OddstableLeagueSortOrder Build(
            IEnumerable<ClientLeagueOrder> sportTreeOrder,
            IEnumerable<ClientOddstableLeagueOrder> oddstableOrder)
        {
            var oddstable = (oddstableOrder ?? Enumerable.Empty<ClientOddstableLeagueOrder>()).ToList();

            return new OddstableLeagueSortOrder(
                (sportTreeOrder ?? Enumerable.Empty<ClientLeagueOrder>())
                    .GroupBy(c => (c.SportId, c.SegmentId, c.LeagueId))
                    .ToDictionary(g => g.Key, g => g.First().SortOrder),
                oddstable
                    .GroupBy(c => (c.SportId, c.SegmentId, c.LeagueId))
                    .ToDictionary(g => g.Key, g => g.First().SortOrder),
                oddstable
                    .GroupBy(c => c.SportId)
                    .ToDictionary(g => g.Key, g => g.Max(c => c.SortOrder)));
        }

        public int Of(int sportId, int segmentId, int leagueId)
        {
            var key = (sportId, segmentId, leagueId);

            if (!_lastConfiguredBySport.TryGetValue(sportId, out var lastConfigured))
                return SportTreeOrder(key);

            if (_configured.TryGetValue(key, out var configured))
                return configured;

            // Shift past every configured league, so the unconfigured ones sort after them whatever
            // the two orders' numbering looks like.
            return lastConfigured + Math.Max(SportTreeOrder(key), 1);
        }

        private int SportTreeOrder((int SportId, int SegmentId, int LeagueId) key)
        {
            return _sportTree.TryGetValue(key, out var order) && order != 0 ? order : DefaultOrder;
        }

        public static string CacheKey(Guid clientId)
        {
            return $"{RowsCachePrefix}{clientId.ToString().ToLower()}";
        }

        // Evicts the cached Oddstable rows and every cached list response built from them. With a
        // client id only that client's entries go, so one Back Office save does not make every other
        // client's lists rebuild from Mongo. Without a usable client id (a message that did not carry
        // one) everything goes, which is slower but never leaves a stale order behind.
        public static void PurgeCache(IMemCache cache, string clientId)
        {
            if (Guid.TryParse(clientId, out var id) && id != Guid.Empty)
            {
                cache.DeleteCache(CacheKey(id));

                var upperId = id.ToString().ToUpper();
                foreach (var prefix in ResponseCachePrefixes)
                    cache.DeleteCacheByPrefix($"{prefix}:{upperId}:");

                return;
            }

            cache.DeleteCacheByPrefix(RowsCachePrefix);

            foreach (var prefix in ResponseCachePrefixes)
                cache.DeleteCacheByPrefix($"{prefix}:");
        }
    }
}
