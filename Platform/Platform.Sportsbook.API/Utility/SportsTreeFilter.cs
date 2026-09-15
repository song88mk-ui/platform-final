using System;
using System.Linq;
using System.Linq.Expressions;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;

namespace Platform.Sportsbook.API.Utility
{
    // Predicate for the sports tree query in SportsbookRepository.GetSports. It lives here as a pure
    // expression so its semantics can be unit-tested without a Mongo instance (GM-771).
    public static class SportsTreeFilter
    {
        // The tree has to mirror what the Early, Today and Live tabs actually show.
        //
        // The live endpoint (SportsbookRepository.GetLiveFixtures) filters on status alone and applies
        // no date bound, so a fixture that is in progress is always rendered no matter when it started.
        // The tree used to apply the local-day start bound to every fixture, live ones included, so any
        // in-progress fixture that started before the player's local midnight was dropped from the tree
        // while it kept rendering in the Today and Live tabs. Continuously running eSports segments
        // (EA Sports FC, NBA2K) are made almost entirely of such fixtures, so they vanished from the
        // tree at 00:00 local.
        //
        // Prematch fixtures keep the bound: those are the ones the Today and Early tabs page by date.
        public static Expression<Func<Fixture, bool>> Build(int[] fixtureTypeIds, DateTime localDayStartUtc)
        {
            return c => fixtureTypeIds.Contains(c.FiTyId)
                        && (c.FiStId == (int)FixtureStatus.Live
                            || (c.FiStId == (int)FixtureStatus.Prematch && c.FiStDa > localDayStartUtc));
        }
    }
}
