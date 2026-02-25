using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.Wallet.API.Telemetry
{
    public class Instrumentation : IDisposable
    {
        internal const string ActivitySourceName = "Platform.Wallet.API";
        internal const string MeterName = "Platform.Wallet.API";

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
