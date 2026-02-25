
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.Job.Resettlement.Telemetry
{
    public class Instrumentation : IDisposable
    {
        internal const string ActivitySourceName = "Platform.Job.Resettlement";
        internal const string MeterName = "Platform.Job.Resettlement";

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
