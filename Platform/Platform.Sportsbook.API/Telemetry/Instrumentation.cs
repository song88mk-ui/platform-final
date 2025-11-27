using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.Sportsbook.API.Telemetry
{
    public class Instrumentation : IDisposable
    {
        internal const string ActivitySourceName = "Platform.Sportsbook.API";
        internal const string MeterName = "Platform.Sportsbook.API";

        private readonly Meter meter;

        public Instrumentation()
        {
            var version = "latest";
            this.ActivitySource = new ActivitySource(ActivitySourceName, version);
            this.meter = new Meter(MeterName, version);
        }

        public ActivitySource ActivitySource { get; }

        public void Dispose()
        {
            this.ActivitySource.Dispose();
            this.meter.Dispose();
        }

    }
}
