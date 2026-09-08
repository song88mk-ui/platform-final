using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

#nullable enable

namespace Platform.Placement.Servicev2.Telemetry
{

    public class Instrumentation : IDisposable
    {

        internal const string ActivitySourceName = "Platform.Placement.Service";
        internal const string MeterName = "Platform.Placement.Service";

        private readonly Meter meter;


        public Instrumentation()
        {
            string? version = "latest";
            this.ActivitySource = new ActivitySource(ActivitySourceName, version);
            this.meter = new Meter(MeterName, version);

            this.BetPlacementCounter = this.meter.CreateCounter<long>("bet.placement.call", "count", "Number of bet placement calls");
        }

        public ActivitySource ActivitySource { get; }
        public Counter<long> BetPlacementCounter { get; }

        public void Dispose()
        {
            this.ActivitySource.Dispose();
            this.meter.Dispose();
        }

    }

}
#nullable disable
