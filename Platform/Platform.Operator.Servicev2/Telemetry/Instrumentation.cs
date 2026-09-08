using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.Operator.Servicev2.Telemetry
{

    public class Instrumentation : IDisposable
    {

        internal const string ActivitySourceName = "Platform.Operator.Servicev2";
        internal const string MeterName = "Platform.Operator.Service";

        private readonly Meter meter;


        public Instrumentation()
        {
            string? version = "latest";
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
