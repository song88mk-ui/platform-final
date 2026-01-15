using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.WebSocket.API2.Telemetry
{
    public class Instrumentation : IDisposable
    {
        internal const string ActivitySourceName = "Platform.WebSocket.API";
        internal const string MeterName = "Platform.WebSocket.API";

        private readonly Meter meter;

        public Instrumentation()
        {
            var version = "latest";
            this.ActivitySource = new ActivitySource(ActivitySourceName, version);
            this.meter = new Meter(MeterName, version);

            EventCounter = this.meter.CreateCounter<long>("event", "count", "help");
        }

        public ActivitySource ActivitySource { get; }
        public Counter<long> EventCounter { get; }

        public void Dispose()
        {
            this.ActivitySource.Dispose();
            this.meter.Dispose();
        }

    }
}
