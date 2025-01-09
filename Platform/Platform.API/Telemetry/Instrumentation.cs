using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.API.Telemetry
{
    public class Instrumentation : IDisposable
    {
        public const string ACTIVITY_SOURCE_NAME = "Platform.API";
        public const string METER_NAME = "Platform.API";
        public const string VERSION = "latest";

        private readonly Meter _meter;

        public Instrumentation()
        {
            ActivitySource = new ActivitySource(ACTIVITY_SOURCE_NAME, VERSION);
            _meter = new Meter(METER_NAME, VERSION);
        }

        public void Dispose()
        {
            ActivitySource.Dispose();
            _meter.Dispose();
        }

        public ActivitySource ActivitySource { get; }
    }
}
