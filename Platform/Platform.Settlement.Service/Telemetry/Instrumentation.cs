using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.Settlement.Service.Telemetry
{

    public class Instrumentation : IDisposable
    {

        internal const string ActivitySourceName = "Platform.Settlement.Service";
        internal const string MeterName = "Platform.Settlement.Service";

        private readonly Meter meter;


        public Instrumentation()
        {
            string version = "latest";
            this.ActivitySource = new ActivitySource(ActivitySourceName, version);
            this.meter = new Meter(MeterName, version);

            this.OnBetSettlementCounter = this.meter.CreateCounter<long>("bet.settlement", "count", "Number of messages settled.");
            this.OnBetCancellationCounter = this.meter.CreateCounter<long>("bet.cancellation", "count", "Number of messages cancelled.");
            this.OnBetBackOfficeCancellationCounter = this.meter.CreateCounter<long>("bet.backoffice.cancellation", "count", "Number of BO cancelled.");
            this.OnBetBackOfficeManualSettlementCounter = this.meter.CreateCounter<long>("bet.backoffice.manual.settlement", "count", "Number of BO manual settled.");

            this.OnBetSettlementDurationHistogram = this.meter.CreateHistogram<double>("bet.settlement.duration", "ms", "Duration for bet settlement work.");
            this.OnBetCancellationDurationHistogram = this.meter.CreateHistogram<double>("bet.cancellation.duration", "ms", "Duration for bet cancellation work.");
            this.OnBetBackOfficeCancellationDurationHistogram = this.meter.CreateHistogram<double>("bet.backoffice.cancellation.duration", "ms", "Duration for bo cancellation work.");
            this.OnBetBackOfficeManualSettlementDurationHistogram = this.meter.CreateHistogram<double>("bet.backoffice.manual.settlement.duration", "ms", "Duration for bo manual settlement work.");
        }

        public ActivitySource ActivitySource { get; }
        public Counter<long> OnBetSettlementCounter { get; }
        public Counter<long> OnBetCancellationCounter { get; }
        public Counter<long> OnBetBackOfficeCancellationCounter { get; }
        public Counter<long> OnBetBackOfficeManualSettlementCounter { get; }
        public Histogram<double> OnBetSettlementDurationHistogram { get; }
        public Histogram<double> OnBetCancellationDurationHistogram { get; }
        public Histogram<double> OnBetBackOfficeCancellationDurationHistogram { get; }
        public Histogram<double> OnBetBackOfficeManualSettlementDurationHistogram { get; }

        public void Dispose()
        {
            this.ActivitySource.Dispose();
            this.meter.Dispose();
        }

    }

}
