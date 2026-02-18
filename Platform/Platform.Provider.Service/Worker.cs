using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Platform.Provider.Service.Repository;
using Platform.Provider.Service.Telemetry;
using Platform.Shared.MessageModels;
using Platform.Shared.Providers;
using Platform.Shared.Utility;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Repositories;

namespace Platform.Provider.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;


        private readonly ActivitySource _activitySource;
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
        private readonly Counter<long> _betCancellationCounter;

        public Worker(ILogger<Worker> logger,
            IMessageBroker messageBroker,
            IBettingRepository bettingRepository,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            Instrumentation instrumentation)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _bettingRepository = bettingRepository;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _activitySource = instrumentation.ActivitySource;
            _betCancellationCounter = instrumentation.BetCancellationCounter;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service");
            if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]))
            {
                try
                {
                    _logger.LogInformation("Application Starting");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "start error");
                }

            }

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping service");
            if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]))
            {
                _logger.LogInformation("stopping MTS");
            }

            _logger.LogInformation("Disconnecting from message broker");
            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connecting to RabbitMQ Broker");
            _messageBroker.Connect();

            _logger.LogInformation("Connected. Processing incoming messages");
            _messageBroker.MessageReceived += MessageReceivedEvent;


            _logger.LogInformation("Connecting to Ably");
            var key = _configuration["Custom:Ably:ApiKey"];
            var options = new ClientOptions
            {
                Key = key,
                Environment = "geniussports",
                FallbackHosts = new string[]
                {
                    "geniussports-a-fallback.ably-realtime.com",
                    "geniussports-b-fallback.ably-realtime.com",
                    "geniussports-c-fallback.ably-realtime.com",
                    "geniussports-d-fallback.ably-realtime.com",
                    "geniussports-e-fallback.ably-realtime.com"
                }
            };

            var ably = new AblyRealtime(options);

            ably.Connection.On(state =>
            {
                _logger.LogInformation("Ably connection state: {State}", state.Current);
            });

            var channelName = _configuration["Custom:Ably:Channel"];
            var channel = ably.Channels.Get(channelName);

            channel.Subscribe(message =>
            {
                try
                {
                    if (string.IsNullOrEmpty(message.Data?.ToString()))
                        return;

                    HandleIncomingMessage(message.Data.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Ably message");
                }
            });

            _logger.LogInformation("Subscribed to Ably channel: {Channel}", channelName);

            return Task.CompletedTask;
        }
        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            if (_betCancellationCounter.Enabled)
                _betCancellationCounter.Add(1);

            var parentContext = Propagator.Extract(default, e.BasicProperties, this.ExtractTraceFromBasicProperties);
            Baggage.Current = parentContext.Baggage;

            using (var activity = _activitySource.StartActivity("process.provider",
                        ActivityKind.Consumer, parentContext.ActivityContext))
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var betProviderFactory = scope.ServiceProvider.GetRequiredService<IBetProviderFactory>();

                    activity?.SetTag("message.type", messageType);

                    switch (messageType)
                    {
                        case "OnBetTimeout":
                            ProcessMessage<OnBetTimeout>(payload, msg =>
                                ProcessWithProvider(
                                    msg,
                                    msg.ChannelTypeId,
                                    betProviderFactory,
                                    correlationId,
                                    (payload, provider, correlationId) => _bettingRepository.ProcessBetCancellation(payload.BetId, payload.LimitId, payload.ClientCode, provider, correlationId)
                                ));
                            break;

                        case "OnBetBackOfficeCancellation":
                            ProcessMessage<OnBetBackofficeCancellation>(payload, msg =>
                                ProcessWithProvider(
                                    msg,
                                    msg.ChannelTypeId,
                                    betProviderFactory,
                                    correlationId,
                                    (payload, provider, correlationId) => _bettingRepository.ProcessBetCancellation(payload.BetId, payload.LimitId, payload.ClientCode, provider, correlationId)
                                ));
                            break;

                        case "OnBetCancellation":
                            ProcessMessage<OnBetSettlement>(payload, msg =>
                                ProcessWithProvider(
                                    msg,
                                    msg.ChannelTypeId,
                                    betProviderFactory,
                                    correlationId,
                                    (payload, provider, correlationId) => _bettingRepository.ProcessBetCancellation(payload.Bet.BetId, payload.Bet.LimitId, payload.Bet.ClientCode, provider, correlationId)
                                ));
                            break;

                        case "OnBetSettlement":

                            _logger.LogInformation($"OnBetSettlement received: {payload}");

                            ProcessMessage<OnBetSettlement>(payload, msg =>
                                ProcessWithProvider(
                                    msg,
                                    msg.ChannelTypeId,
                                    betProviderFactory,
                                    correlationId,
                                    (payload, provider, correlationId) => _bettingRepository.ProcessBetSettlement(payload, provider, correlationId)
                                ));
                            break;
                    }
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError(ex, "error on message received");
                }
        }
        private void ProcessMessage<T>(string payload, Action<T> processor)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;

            var message = JsonSerializer.Deserialize<T>(payload);
            if (message != null)
            {
                processor(message);
            }
        }
        private void ProcessWithProvider<T>(T payload, int channelTypeId, IBetProviderFactory factory, string correlationId, Action<T, IBetProvider, string> handler)
        {
            var provider = factory.Resolve(channelTypeId);
            handler(payload, provider, correlationId);
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
            }
            catch (Exception) { }

            return Enumerable.Empty<string>();
        }

        private void HandleIncomingMessage(string payload)
        {
            _logger.LogInformation($"process.bet.assessment: {payload}");

            using var scope = _scopeFactory.CreateScope();
            var betProviderFactory = scope.ServiceProvider.GetRequiredService<IBetProviderFactory>();

            using var activity = _activitySource.StartActivity("process.bet.assessment", ActivityKind.Consumer);

            try
            {
                var message = JsonSerializer.Deserialize<GTSAssessmentResponseDTO>(payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (message == null)
                    return;

                Guid.TryParse(message.BetId, out var betId);

                var delayedMessage = string.Empty;
                if (!message.IsBetAllowed)
                    if (message.Legs?.Any() ?? false)
                        delayedMessage = message.Legs.FirstOrDefault(l => l.RejectReason != null).RejectReason?.ReasonMessage ?? string.Empty;


                var onBetPlacement = new OnBetPlacement();
                onBetPlacement.ChannelTypeId = (int)ChannelType.BG;
                onBetPlacement.DelayedBetAcceptance = message.IsBetAllowed ? BetAcceptance.Accepted : BetAcceptance.Rejected;
                onBetPlacement.DelayedMessage = delayedMessage;
                onBetPlacement.Bet = new Shared.Data.Betting.Entities.Bet();
                onBetPlacement.Bet.BetId = betId;
                onBetPlacement.BetSelections = new List<BetSelectionMessageExtenstion>();

                _messageBroker.Publish(onBetPlacement, "OnBetPlacement", $"r.bet.placement", Guid.NewGuid().ToString().ToUpper());

                _logger.LogInformation($"Published bet assessment to RabbitMQ | BetId={onBetPlacement.Bet?.BetId}");

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, $"Failed to process Ably message: {payload}");
            }
        }
    }
}
