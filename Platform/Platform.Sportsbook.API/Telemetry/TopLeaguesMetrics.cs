using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Platform.Shared.Enums;

namespace Platform.Sportsbook.API.Telemetry
{
    // Latency breakdown for POST v1/sportsbook/topleagues (SportsbookRepository.GetTopLeagues):
    //   phase "mongo_fetch" + the fixtures histogram -> cost of reading the sport's fixtures from Mongo
    //   phases "translations", "league_config", "ranking" -> in-memory work and the cached SQL config reads
    //   requests counter (cache, outcome, concurrent_miss) -> hit ratio, empty results, duplicate misses
    // End-to-end latency, cache hits included, is already exported by the ASP.NET Core instrumentation
    // as http.server.request.duration with http.route "v1/sportsbook/topleagues".
    //
    // Tags stay low-cardinality: sport.id and language "en"/"other" (non-EN requests also read the
    // per-language translation lists, so their cost differs). Never tag with the client id or the cache key.
    public sealed class TopLeaguesMetrics
    {
        public const string PhaseMongoFetch = "mongo_fetch";
        public const string PhaseTranslations = "translations";
        public const string PhaseLeagueConfig = "league_config";
        public const string PhaseRanking = "ranking";
        public const string PhaseTotal = "total";

        private readonly Counter<long> _requests;
        private readonly Histogram<double> _phaseDuration;
        private readonly Histogram<long> _fixtures;

        // Misses currently computing, per response cache key. A miss that starts while another one for
        // the same key is in flight repeats the same Mongo read, because the response cache is filled
        // only when a miss finishes.
        private readonly ConcurrentDictionary<string, int> _inFlight = new ConcurrentDictionary<string, int>();

        public TopLeaguesMetrics(Meter meter)
        {
            _requests = meter.CreateCounter<long>("sportsbook.topleagues.requests", "count", "GetTopLeagues calls by cache result and outcome");
            _phaseDuration = meter.CreateHistogram<double>("sportsbook.topleagues.phase.duration", "ms", "GetTopLeagues cache-miss time per phase");
            _fixtures = meter.CreateHistogram<long>("sportsbook.topleagues.fixtures", "count", "Fixtures read from Mongo per GetTopLeagues cache miss");
        }

        public void RecordHit(int sportId, LanguageCode languageCode, Activity activity)
        {
            activity?.SetTag("topleagues.cache", "hit");

            _requests.Add(1,
                new KeyValuePair<string, object>("cache", "hit"),
                new KeyValuePair<string, object>("sport.id", sportId),
                new KeyValuePair<string, object>("language", LanguageGroup(languageCode)));
        }

        public Miss StartMiss(string cacheKey, int sportId, LanguageCode languageCode, Activity activity)
        {
            var concurrent = _inFlight.AddOrUpdate(cacheKey, 1, (_, count) => count + 1) > 1;

            return new Miss(this, cacheKey, sportId, LanguageGroup(languageCode), concurrent, activity);
        }

        private void EndMiss(string cacheKey)
        {
            // Remove the entry once its count drops to zero so the dictionary only holds keys in flight.
            while (_inFlight.TryGetValue(cacheKey, out var count))
            {
                if (count <= 1)
                {
                    if (_inFlight.TryRemove(new KeyValuePair<string, int>(cacheKey, count)))
                        return;
                }
                else if (_inFlight.TryUpdate(cacheKey, count - 1, count))
                    return;
            }
        }

        private static string LanguageGroup(LanguageCode languageCode) => languageCode == LanguageCode.EN ? "en" : "other";

        // One cache miss. Call EndPhase after each phase and Complete before returning; Dispose records
        // the total, the outcome ("results", "empty", or "error" when Complete was never reached) and
        // releases the in-flight slot, so an exception still leaves the counts right.
        public sealed class Miss : IDisposable
        {
            private readonly TopLeaguesMetrics _owner;
            private readonly string _cacheKey;
            private readonly KeyValuePair<string, object> _sportTag;
            private readonly KeyValuePair<string, object> _languageTag;
            private readonly Activity _activity;
            private readonly long _start;
            private long _phaseStart;
            private string _outcome = "error";
            private bool _disposed;

            internal Miss(TopLeaguesMetrics owner, string cacheKey, int sportId, string languageGroup, bool concurrent, Activity activity)
            {
                _owner = owner;
                _cacheKey = cacheKey;
                _sportTag = new KeyValuePair<string, object>("sport.id", sportId);
                _languageTag = new KeyValuePair<string, object>("language", languageGroup);
                _activity = activity;
                Concurrent = concurrent;
                _start = Stopwatch.GetTimestamp();
                _phaseStart = _start;

                _activity?.SetTag("topleagues.cache", "miss");
                _activity?.SetTag("topleagues.concurrent_miss", concurrent);
            }

            public bool Concurrent { get; }

            public void EndPhase(string phase)
            {
                var now = Stopwatch.GetTimestamp();
                var elapsedMs = Stopwatch.GetElapsedTime(_phaseStart, now).TotalMilliseconds;
                _phaseStart = now;

                _owner._phaseDuration.Record(elapsedMs, new KeyValuePair<string, object>("phase", phase), _sportTag, _languageTag);
                _activity?.SetTag($"topleagues.{phase}_ms", elapsedMs);
            }

            public void RecordFixtures(int count)
            {
                _owner._fixtures.Record(count, _sportTag, _languageTag);
                _activity?.SetTag("topleagues.fixtures", count);
            }

            public void Complete(int resultCount)
            {
                _outcome = resultCount > 0 ? "results" : "empty";
                _activity?.SetTag("topleagues.results", resultCount);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;

                var totalMs = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;

                _owner._phaseDuration.Record(totalMs, new KeyValuePair<string, object>("phase", PhaseTotal), _sportTag, _languageTag);
                _owner._requests.Add(1,
                    new KeyValuePair<string, object>("cache", "miss"),
                    new KeyValuePair<string, object>("outcome", _outcome),
                    new KeyValuePair<string, object>("concurrent_miss", Concurrent),
                    _sportTag,
                    _languageTag);
                _activity?.SetTag("topleagues.total_ms", totalMs);

                _owner.EndMiss(_cacheKey);
            }
        }
    }
}
