using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Platform.WebSocket.API.Telemetry
{
    public class Instrumentation : IDisposable
    {
        internal const string ActivitySourceName = "Platform.WebSocket.API";
        internal const string MeterName = "Platform.WebSocket.API";

        private readonly Meter meter;

        public Instrumentation()
        {
            this.ActivitySource = new ActivitySource(ActivitySourceName, "latest");
            this.meter = new Meter(MeterName, "latest");
        }

        public ActivitySource ActivitySource { get; }

        public void Dispose()
        {
            this.ActivitySource.Dispose();
            this.meter.Dispose();
        }

    }
}
