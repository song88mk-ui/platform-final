using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Shared.Data.Sportsbook.SQLEntities;
using Platform.Shared.Models;
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

            return ShiftPast(lastConfigured, SportTreeOrder(key));
        }

        // Same rule as Of, for a caller that already holds the league's Sport Tree order. A sport with
        // no Oddstable rows gets that order back unchanged, so a client that has not configured the
        // Oddstable sees no difference.
        public int Apply(int sportId, int segmentId, int leagueId, int sportTreeOrder)
        {
            if (!_lastConfiguredBySport.TryGetValue(sportId, out var lastConfigured))
                return sportTreeOrder;

            if (_configured.TryGetValue((sportId, segmentId, leagueId), out var configured))
                return configured;

            return ShiftPast(lastConfigured, sportTreeOrder);
        }

        // Shift past every configured league, so the unconfigured ones sort after them whatever the
        // two orders' numbering looks like. 0 means "not set" in ClientLeagueOrder.
        private static int ShiftPast(int lastConfigured, int sportTreeOrder)
        {
            return lastConfigured + Math.Max(sportTreeOrder != 0 ? sportTreeOrder : DefaultOrder, 1);
        }

        private int SportTreeOrder((int SportId, int SegmentId, int LeagueId) key)
        {
            return _sportTree.TryGetValue(key, out var order) && order != 0 ? order : DefaultOrder;
        }

        // The per-client league order the odds engine's websocket service loads (it calls
        // GetClientFixtureSortOrder at startup) and stamps on every fixture it pushes as SoOr. Platform
        // Web places a league that first appears through a push by that number, so it has to be the
        // same order the lists use. Before GM-851 it was the Sport Tree order only, which put a newly
        // live league in the wrong place whenever the Oddstable order differed.
        //
        // The rows are the Sport Tree rows, then the global tier rows (plus 1000) for leagues the client
        // has no row for, as before. Where a sport has Oddstable rows, every row is moved to its
        // Oddstable position, and a configured league with no Sport Tree or tier row is added, because
        // the websocket service falls back to 1000 for a league it has no row for.
        public static List<ClientFixtureSortOrderDTO> BuildClientFixtureSortOrder(
            IEnumerable<ClientLeagueOrder> sportTreeOrder,
            IEnumerable<TierLeagueOrder> tierOrder,
            IEnumerable<ClientOddstableLeagueOrder> oddstableOrder)
        {
            var sportTreeRows = (sportTreeOrder ?? Enumerable.Empty<ClientLeagueOrder>()).ToList();
            var oddstableRows = (oddstableOrder ?? Enumerable.Empty<ClientOddstableLeagueOrder>()).ToList();

            var result = sportTreeRows.Select(s => new ClientFixtureSortOrderDTO
            {
                SportId = s.SportId,
                SegmentId = s.SegmentId,
                LeagueId = s.LeagueId,
                SortOrder = s.SortOrder
            }).ToList();

            var present = result.Select(r => (r.SportId, r.SegmentId, r.LeagueId)).ToHashSet();

            if (tierOrder != null)
            {
                result.AddRange(tierOrder
                    .Where(t => !present.Contains((t.SportId, t.SegmentId, t.LeagueId)))
                    .Select(t => new ClientFixtureSortOrderDTO
                    {
                        SportId = t.SportId,
                        SegmentId = t.SegmentId,
                        LeagueId = t.LeagueId,
                        SortOrder = t.SortOrder + 1000
                    }).ToList());
            }

            if (oddstableRows.Count == 0)
                return result;

            var order = Build(sportTreeRows, oddstableRows);

            foreach (var row in result)
                row.SortOrder = order.Apply(row.SportId, row.SegmentId, row.LeagueId, row.SortOrder);

            present = result.Select(r => (r.SportId, r.SegmentId, r.LeagueId)).ToHashSet();

            foreach (var configured in oddstableRows.GroupBy(c => (c.SportId, c.SegmentId, c.LeagueId)).Select(g => g.First()))
            {
                if (present.Add((configured.SportId, configured.SegmentId, configured.LeagueId)))
                {
                    result.Add(new ClientFixtureSortOrderDTO
                    {
                        SportId = configured.SportId,
                        SegmentId = configured.SegmentId,
                        LeagueId = configured.LeagueId,
                        SortOrder = configured.SortOrder
                    });
                }
            }

            return result;
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
