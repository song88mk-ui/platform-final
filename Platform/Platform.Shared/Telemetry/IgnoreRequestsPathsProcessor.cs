using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Platform.Shared.Telemetry
{
    public class IgnoreRequestsPathsProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public IgnoreRequestsPathsProcessor(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(ITelemetry telemetry)
        {
            // filter syntethic requests
            if (!string.IsNullOrEmpty(telemetry.Context.Operation.SyntheticSource)) { return; }

            // filter out paths to
            if (telemetry is RequestTelemetry request &&
                (request.Url.AbsolutePath.StartsWith("/v1/heartbeat") ||
                 request.Url.AbsolutePath.StartsWith("/swagger") ||
                 request.Url.AbsolutePath.StartsWith("/healthz"))) { return; }

            // filter out dependency calls
            if (telemetry is DependencyTelemetry dependency &&
                (dependency.Name.EndsWith("/v1/traces") ||
                 dependency.Name.EndsWith("/v1/metrics"))) { return; }

            _next.Process(telemetry);
        }
    }
}
