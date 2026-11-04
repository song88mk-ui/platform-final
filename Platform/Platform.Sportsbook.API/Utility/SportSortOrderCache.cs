using System;
using Platform.Shared.Utility;

namespace Platform.Sportsbook.API.Utility
{
    // Cache keys for the Sport Settings order rows Back Office edits: sport order (ClientSportOrder),
    // segment order (ClientSegmentOrder) and league order inside a segment (ClientLeagueOrder).
    //
    // Segment and league order are cached per client, so the purge has to go by prefix. An exact
    // delete of "seo" or "leo" never matches a "seo-{clientId}" key, which left a saved order stale
    // until the 24 hour expiry or a pod restart (BO-152). Back Office sends a random ClientId on
    // OnSportSortOrderChange, so the purge cannot be narrowed to one client yet.
    //
    // It lives here as a plain class so the purge can be unit-tested without a Mongo or SQL instance.
    public static class SportSortOrderCache
    {
        public const string SportOrderKey = "so";

        private const string SegmentOrderPrefix = "seo-";
        private const string LeagueOrderPrefix = "leo-";

        public static string SegmentOrderKey(Guid clientId) => $"{SegmentOrderPrefix}{clientId.ToString().ToLower()}";

        public static string LeagueOrderKey(Guid clientId) => $"{LeagueOrderPrefix}{clientId.ToString().ToLower()}";

        public static void PurgeCache(IMemCache cache)
        {
            cache.DeleteCache(SportOrderKey);
            cache.DeleteCacheByPrefix(SegmentOrderPrefix);
            cache.DeleteCacheByPrefix(LeagueOrderPrefix);
        }
    }
}
