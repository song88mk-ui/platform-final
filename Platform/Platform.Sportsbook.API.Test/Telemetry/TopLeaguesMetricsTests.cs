using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Platform.Shared.Enums;
using Platform.Sportsbook.API.Telemetry;
using Xunit;

namespace Platform.Sportsbook.API.Test.Telemetry
{
    // The tags TopLeaguesMetrics emits (cache, outcome, concurrent_miss, phase) are what dashboards and
    // alerts query on, so renaming or dropping one silently breaks them.
    public class TopLeaguesMetricsTests : IDisposable
    {
        private readonly Meter _meter = new Meter($"TopLeaguesMetricsTests.{Guid.NewGuid()}");
        private readonly MeterListener _listener = new MeterListener();
        private readonly List<(string Instrument, double Value, Dictionary<string, object> Tags)> _measurements = new();
        private readonly TopLeaguesMetrics _metrics;

        public TopLeaguesMetricsTests()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter == _meter)
                    listener.EnableMeasurementEvents(instrument);
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Capture(instrument, value, tags));
            _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Capture(instrument, value, tags));
            _listener.Start();

            _metrics = new TopLeaguesMetrics(_meter);
        }

        public void Dispose()
        {
            _listener.Dispose();
            _meter.Dispose();
        }

        private void Capture(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object>> tags)
        {
            var map = new Dictionary<string, object>();
            foreach (var tag in tags)
                map[tag.Key] = tag.Value;
            _measurements.Add((instrument.Name, value, map));
        }

        private List<Dictionary<string, object>> Requests() =>
            _measurements.Where(m => m.Instrument == "sportsbook.topleagues.requests").Select(m => m.Tags).ToList();

        [Fact]
        public void Hit_CountsOneRequestTaggedHit()
        {
            _metrics.RecordHit(1, LanguageCode.EN, null);

            var request = Assert.Single(Requests());
            Assert.Equal("hit", request["cache"]);
            Assert.Equal(1, request["sport.id"]);
            Assert.Equal("en", request["language"]);
        }

        [Fact]
        public void Miss_RecordsEachPhaseTheTotalAndTheOutcome()
        {
            using (var miss = _metrics.StartMiss("ntl:A:ZH:1", 1, LanguageCode.ZH, null))
            {
                miss.EndPhase(TopLeaguesMetrics.PhaseMongoFetch);
                miss.RecordFixtures(42);
                miss.EndPhase(TopLeaguesMetrics.PhaseRanking);
                miss.Complete(15);
            }

            var phases = _measurements.Where(m => m.Instrument == "sportsbook.topleagues.phase.duration").Select(m => (string)m.Tags["phase"]).ToList();
            Assert.Equal(new[] { TopLeaguesMetrics.PhaseMongoFetch, TopLeaguesMetrics.PhaseRanking, TopLeaguesMetrics.PhaseTotal }, phases);

            Assert.Equal(42, _measurements.Single(m => m.Instrument == "sportsbook.topleagues.fixtures").Value);

            var request = Assert.Single(Requests());
            Assert.Equal("miss", request["cache"]);
            Assert.Equal("results", request["outcome"]);
            Assert.False((bool)request["concurrent_miss"]);
            Assert.Equal("other", request["language"]);
        }

        [Fact]
        public void Miss_WithNoResults_IsTaggedEmpty()
        {
            using (var miss = _metrics.StartMiss("ntl:A:EN:1", 1, LanguageCode.EN, null))
                miss.Complete(0);

            Assert.Equal("empty", Assert.Single(Requests())["outcome"]);
        }

        [Fact]
        public void Miss_DisposedWithoutComplete_IsTaggedError()
        {
            // The repository has no try/catch around the Mongo read: an exception skips Complete,
            // and the using block still records the miss.
            var miss = _metrics.StartMiss("ntl:A:EN:1", 1, LanguageCode.EN, null);
            miss.Dispose();
            miss.Dispose();

            Assert.Equal("error", Assert.Single(Requests())["outcome"]);
        }

        [Fact]
        public void OverlappingMisses_OnTheSameKey_AreConcurrent_OnOtherKeysAreNot()
        {
            var first = _metrics.StartMiss("ntl:A:EN:1", 1, LanguageCode.EN, null);
            var second = _metrics.StartMiss("ntl:A:EN:1", 1, LanguageCode.EN, null);
            var otherKey = _metrics.StartMiss("ntl:A:EN:2", 2, LanguageCode.EN, null);

            Assert.False(first.Concurrent);
            Assert.True(second.Concurrent);
            Assert.False(otherKey.Concurrent);

            first.Dispose();
            second.Dispose();
            otherKey.Dispose();
        }

        [Fact]
        public void FinishedMiss_ReleasesTheKey()
        {
            _metrics.StartMiss("ntl:A:EN:1", 1, LanguageCode.EN, null).Dispose();

            using var next = _metrics.StartMiss("ntl:A:EN:1", 1, LanguageCode.EN, null);

            Assert.False(next.Concurrent);
        }
    }
}
