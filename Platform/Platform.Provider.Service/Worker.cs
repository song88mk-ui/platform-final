using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Provider.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Providers;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;

namespace Platform.Provider.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger,
            IMessageBroker messageBroker,
            IBettingRepository bettingRepository,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _bettingRepository = bettingRepository;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
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

            return Task.CompletedTask;
        }
        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var betProviderFactory = scope.ServiceProvider.GetRequiredService<IBetProviderFactory>();

                switch (messageType)
                {
                    case "OnBetTimeout":
                        ProcessMessage<OnBetTimeout>(payload, msg =>
                            ProcessWithProvider(
                                msg,
                                msg.ChannelTypeId,
                                betProviderFactory,
                                correlationId,
                                (payload, provider, correlationId) => _bettingRepository.ProcessBetCancellation(payload.BetId, provider, correlationId)
                            ));
                        break;

                    case "OnBetBackOfficeCancellation":
                        ProcessMessage<OnBetBackofficeCancellation>(payload, msg =>
                            ProcessWithProvider(
                                msg,
                                msg.ChannelTypeId,
                                betProviderFactory,
                                correlationId,
                                (payload, provider, correlationId) => _bettingRepository.ProcessBetCancellation(payload.BetId, provider, correlationId)
                            ));
                        break;

                    case "OnBetCancellation":
                        ProcessMessage<OnBetSettlement>(payload, msg =>
                            ProcessWithProvider(
                                msg,
                                msg.ChannelTypeId,
                                betProviderFactory,
                                correlationId,
                                (payload, provider, correlationId) => _bettingRepository.ProcessBetCancellation(payload.Bet.BetId, provider, correlationId)
                            ));
                        break;

                    case "OnBetSettlement":
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
            }
            catch (Exception ex)
            {
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
    }
}
