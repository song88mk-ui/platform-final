using System;
using System.Linq;
using System.Linq.Expressions;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;

namespace Platform.Sportsbook.API.Utility
{
    // Fixture predicates that include or exclude virtual sports. They live here as pure expressions so
    // a unit test can render them to a Mongo filter without a Mongo instance, as SportsTreeFilter is.
    //
    // The virtual sport check has to be a Contains on a list. The MongoDB driver (2.11.6) cannot
    // translate a call to an extension method such as c.SpId.IsVirtualSportId(): it throws
    // "Unsupported filter" while building the query, before anything reaches Mongo. Written that way,
    // GetLeagueHighlights, GetLeagueHighlightsFixtures and GetClientSportsLookup threw on every call
    // from GM-103 on. A list Contains renders as $in / $nin.
    public static class VirtualSportFilter
    {
        // Active (prematch or live) match fixtures of non-virtual sports: GetLeagueHighlights and
        // GetLeagueHighlightsFixtures.
        public static Expression<Func<Fixture, bool>> ActiveMatchesExcludingVirtual()
        {
            var virtualSportIds = IntExtensions.GetVirtualSportIds();

            return c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && !virtualSportIds.Contains(c.SpId);
        }

        // Fixtures of virtual sports with a status below 12: GetClientSportsLookup.
        public static Expression<Func<Fixture, bool>> VirtualSportFixtures()
        {
            var virtualSportIds = IntExtensions.GetVirtualSportIds();

            return c => c.FiStId < 12 && virtualSportIds.Contains(c.SpId);
        }
    }
}
