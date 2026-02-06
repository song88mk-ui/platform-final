using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.ClientPlatform.API.Telemetry
{
    public class Instrumentation : IDisposable
    {
        public const string ActivitySourceName = "ClientPlatform.API";
        public const string MeterName = "ClientPlatform.API";
        public const string Version = "latest";

        private readonly Meter _meter;

        public Instrumentation()
        {
            ActivitySource = new ActivitySource(ActivitySourceName, Version);
            _meter = new Meter(MeterName, Version);
        }

        public void Dispose()
        {
            ActivitySource.Dispose();
            _meter.Dispose();
        }

        public ActivitySource ActivitySource { get; }
    }
}
