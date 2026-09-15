using System;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Sportsbook.API.Utility;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // GM-771. The numbers below are taken from the staging recording attached to the ticket on
    // 2026-09-11: a UTC+8 player just after local midnight, with EA Sports FC and NBA2K fixtures
    // still in progress in the Today tab but both segments missing from the Sports Tree.
    public class SportsTreeFilterTests
    {
        private const int Utc8 = 480;

        // 2026-09-11 00:01 in UTC+8.
        private static readonly DateTime NowUtc = new DateTime(2026, 9, 10, 16, 1, 0, DateTimeKind.Utc);

        // The bound the repository passes in, derived the same way GetSports derives it.
        private static readonly DateTime DayStartUtc =
            LocalDayRange.Compute(NowUtc.AddMinutes(Utc8), Utc8, dateIsFromRequest: true).Item1;

        private static readonly int[] TreeTypes = { (int)FixtureType.Match, (int)FixtureType.Outright };

        private static Fixture Make(int status, int type, DateTime startUtc) => new Fixture
        {
            FiId = 1,
            SpId = 3,
            SegId = 3795074,
            LeId = 1,
            FiStId = status,
            FiTyId = type,
            FiStDa = startUtc
        };

        private static bool Matches(Fixture fixture) =>
            SportsTreeFilter.Build(TreeTypes, DayStartUtc).Compile()(fixture);

        // The exact predicate the tree used before this change, for contrast.
        private static bool MatchesOldPredicate(Fixture fixture) =>
            fixture.FiStId < 2 && TreeTypes.Contains(fixture.FiTyId) && fixture.FiStDa > DayStartUtc;

        // 2026-09-10 23:50 UTC+8: an EA Sports FC H2H GG League match that is still in progress at 00:01.
        private static readonly DateTime StartedBeforeLocalMidnight = new DateTime(2026, 9, 10, 15, 50, 0, DateTimeKind.Utc);

        [Fact]
        public void LiveFixtureStartedBeforeLocalMidnight_IsIncluded()
        {
            var fixture = Make((int)FixtureStatus.Live, (int)FixtureType.Match, StartedBeforeLocalMidnight);

            Assert.True(Matches(fixture));
        }

        [Fact]
        public void LiveFixtureStartedBeforeLocalMidnight_WasExcludedByTheOldPredicate()
        {
            // Pins the defect this change fixes: the same in-progress fixture failed the old bound,
            // which is why the segment disappeared from the tree while the Today tab still showed it.
            var fixture = Make((int)FixtureStatus.Live, (int)FixtureType.Match, StartedBeforeLocalMidnight);

            Assert.False(MatchesOldPredicate(fixture));
        }

        [Fact]
        public void LiveFixtureStartedAfterLocalMidnight_IsStillIncluded()
        {
            var fixture = Make((int)FixtureStatus.Live, (int)FixtureType.Match, NowUtc.AddMinutes(-1));

            Assert.True(Matches(fixture));
            Assert.True(MatchesOldPredicate(fixture));
        }

        [Fact]
        public void PrematchFixtureLaterToday_IsIncluded()
        {
            // 2026-09-11 17:00 UTC+8, the scheduled LoL fixture that kept the LoL segment alive.
            var fixture = Make((int)FixtureStatus.Prematch, (int)FixtureType.Match, new DateTime(2026, 9, 11, 9, 0, 0, DateTimeKind.Utc));

            Assert.True(Matches(fixture));
        }

        [Fact]
        public void PrematchFixtureBeforeLocalMidnight_IsStillExcluded()
        {
            // The day bound must keep applying to prematch fixtures, otherwise yesterday's unplayed
            // fixtures would leak into the tree.
            var fixture = Make((int)FixtureStatus.Prematch, (int)FixtureType.Match, StartedBeforeLocalMidnight);

            Assert.False(Matches(fixture));
        }

        [Theory]
        [InlineData((int)FixtureStatus.Suspended)]
        [InlineData((int)FixtureStatus.Ended)]
        [InlineData((int)FixtureStatus.Closed)]
        [InlineData((int)FixtureStatus.Cancelled)]
        public void NonPlayableStatuses_AreExcludedWhateverTheStartDate(int status)
        {
            // Preserves the old "FiStId < 2" gate: only prematch and live reach the tree.
            Assert.False(Matches(Make(status, (int)FixtureType.Match, NowUtc)));
            Assert.False(Matches(Make(status, (int)FixtureType.Match, StartedBeforeLocalMidnight)));
        }

        [Fact]
        public void StatusGate_StillMatchesTheOldFiStIdLessThanTwoGate_ForEveryKnownStatus()
        {
            // The old gate was "FiStId < 2"; the new one names Prematch and Live explicitly.
            // FixtureStatus runs 0 to 11 with no negative members, so the two must agree on every
            // status once the date condition is satisfied. Anything else would be a silent change
            // to which fixtures reach the tree.
            foreach (FixtureStatus status in Enum.GetValues(typeof(FixtureStatus)))
            {
                var futureDated = Make((int)status, (int)FixtureType.Match, NowUtc.AddHours(2));

                Assert.Equal(MatchesOldPredicate(futureDated), Matches(futureDated));
            }
        }

        [Fact]
        public void FixtureTypeOutsideTheTreeTypes_IsExcluded()
        {
            var fixture = Make((int)FixtureStatus.Live, 99, NowUtc);

            Assert.False(Matches(fixture));
        }

        [Fact]
        public void FutureOutright_IsIncluded()
        {
            var fixture = Make((int)FixtureStatus.Prematch, (int)FixtureType.Outright, NowUtc.AddDays(30));

            Assert.True(Matches(fixture));
        }

        [Fact]
        public void EsportsSegmentWithOnlyPreMidnightLiveFixtures_SurvivesTheFilter()
        {
            // The reported scenario end to end: NBA2K eBasketball H2H GG League at 00:01 local has only
            // in-progress matches that tipped off before midnight. Under the old bound the segment
            // contributed no fixtures at all and disappeared from the tree.
            var segment = new[]
            {
                Make((int)FixtureStatus.Live, (int)FixtureType.Match, new DateTime(2026, 9, 10, 15, 40, 0, DateTimeKind.Utc)),
                Make((int)FixtureStatus.Live, (int)FixtureType.Match, new DateTime(2026, 9, 10, 15, 52, 0, DateTimeKind.Utc))
            };

            Assert.Equal(2, segment.Count(Matches));
            Assert.Equal(0, segment.Count(MatchesOldPredicate));
        }

        [Fact]
        public void Filter_IsTranslatableByTheMongoDriver()
        {
            // Build() is handed straight to Builders<Fixture>.Filter.Where in GetSports, so the driver
            // has to be able to turn it into a query document. Render throws if it cannot.
            var filter = Builders<Fixture>.Filter.Where(SportsTreeFilter.Build(TreeTypes, DayStartUtc));

            var rendered = filter.Render(
                BsonSerializer.SerializerRegistry.GetSerializer<Fixture>(),
                BsonSerializer.SerializerRegistry);

            Assert.NotNull(rendered);
            // Pinned: the $or must keep the live branch (FiStId 1) free of any FiStDa condition.
            // If a future edit reintroduces a date bound on live fixtures this assertion fails.
            Assert.Equal(
                @"{ ""FiTyId"" : { ""$in"" : [0, 1] }, ""$or"" : [{ ""FiStId"" : 1 }, { ""FiStId"" : 0, ""FiStDa"" : { ""$gt"" : ISODate(""2026-09-10T15:59:59.999Z"") } }] }",
                rendered.ToString());
        }
    }
}
