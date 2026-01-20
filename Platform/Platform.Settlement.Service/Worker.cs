using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text;
using Platform.Shared.Utility;
using Platform.Settlement.Service.Repository;
using Platform.Settlement.Service.Telemetry;
using Platform.Shared.MessageModels;
using Platform.Shared.Enums;

namespace Platform.Settlement.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;

        private readonly ActivitySource _activitySource;
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IBettingRepository bettingRepository, Instrumentation instrumentation)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _bettingRepository = bettingRepository;
            _activitySource = instrumentation.ActivitySource;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connecting to the broker/RabbitMQ");
            _messageBroker.Connect();

            _logger.LogInformation("Connected. Processing incoming messages");
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }

        private IEnumerable<string> ExtractTraceFromBasicProperties(IBasicProperties props, string key)
        {
          try
          {
            if (props.Headers.TryGetValue(key, out var value))
            {
              var bytes = value as byte[];
              return new[] { Encoding.UTF8.GetString(bytes) };
            }
          } catch (Exception) {}

          return Enumerable.Empty<string>();
        }

        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            var parentContext = Propagator.Extract(default, e.BasicProperties, this.ExtractTraceFromBasicProperties);
            Baggage.Current = parentContext.Baggage;

            var activityName = $"{e.RoutingKey} receive";

            using (var activity = _activitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext))
            {
                try
                {
                    _logger.LogInformation($"{messageType}.{e.RoutingKey}");

                    activity?.SetTag("messaging.system", "rabbitmq");
                    activity?.SetTag("messaging.destination_kind", "queue");
                    activity?.SetTag("messaging.rabbitmq.routing_key", e.RoutingKey);
                    activity?.SetTag("messaging.rabbitmq.type", messageType);

                    switch (messageType)
                    {
                        case "OnBetSettlement":
                            var onBetSettlement = JsonSerializer.Deserialize<OnUOFBetSettlement>(payload);
                            activity.SetTag("selection.id", onBetSettlement.SelectionId);
                            _bettingRepository.ProcessBetSettlement(onBetSettlement, _messageBroker, correlationId);
                            break;

                        case "OnBetCancellation":
                            var onBetCancellation = JsonSerializer.Deserialize<OnUOFBetCancellation>(payload);
                            activity.SetTag("market.id", onBetCancellation.MarketId);
                            _bettingRepository.ProcessBetCancellation(onBetCancellation, _messageBroker, correlationId);
                            break;

                        case "OnBetBackOfficeCancellation":
                            var onBOBetCancellation = JsonSerializer.Deserialize<OnBOBetCancellaton>(payload);
                            _bettingRepository.ProcessBackOfficeBetCancellation(onBOBetCancellation, _messageBroker, correlationId);
                            break;

                        case "OnBetBackOfficeManualSettlement":
                            var onBOBetManualSettlement = JsonSerializer.Deserialize<OnBOBetManualSettlement>(payload);
                            _bettingRepository.ProcessBackOfficeManualSettlement(onBOBetManualSettlement, _messageBroker, correlationId);
                            break;
                        case "OnOddsFeedBetCancellation":
                            var onOFBetCancellation = JsonSerializer.Deserialize<OnOddsFeedBetCancellation>(payload);
                            activity.SetTag("client.id", onOFBetCancellation.ClientId);
                            activity.SetTag("bet.id", onOFBetCancellation.BetId);
                            _bettingRepository.ProcessOddsFeedBetCancellation(onOFBetCancellation, _messageBroker, correlationId);
                            break;
                        case "OnOddsFeedBetCancellationRetry":
                            var onOFBetCancellationRetry = JsonSerializer.Deserialize<OnOddsFeedBetCancellation>(payload);
                            activity.SetTag("client.id", onOFBetCancellationRetry.ClientId);
                            activity.SetTag("bet.id", onOFBetCancellationRetry.BetId);
                            _bettingRepository.ProcessOddsFeedBetCancellation(onOFBetCancellationRetry, _messageBroker, correlationId, false);
                            break;
                    }
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError(ex, "Error processing message");
                }
            }
        }
    }
}
