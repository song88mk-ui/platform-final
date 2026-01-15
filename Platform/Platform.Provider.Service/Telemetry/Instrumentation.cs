using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

#nullable enable

namespace Platform.Provider.Service.Telemetry
{

  public class Instrumentation: IDisposable
  {

    internal const string ActivitySourceName = "Platform.Provider.Service";
    internal const string MeterName = "Platform.Provider.Service";

    private readonly Meter meter;


    public Instrumentation()
    {
      string? version = "latest";
      this.ActivitySource = new ActivitySource(ActivitySourceName, version);
      this.meter = new Meter(MeterName, version);

      this.BetCancellationCounter = this.meter.CreateCounter<long>("bet.cancellation.call", "count", "Number of bet cancellation calls");
    }

    public ActivitySource ActivitySource { get; }
    public Counter<long> BetCancellationCounter { get; }

    public void Dispose()
    {
      this.ActivitySource.Dispose();
      this.meter.Dispose();
    }

  }

}
#nullable disable
