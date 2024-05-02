using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Client.TopMarket.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Client.TopMarket
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IBettingRepository bettingRepository)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _bettingRepository = bettingRepository;
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
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            _messageBroker.ConnectWithManualAck();
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }

        private async void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            try
            {
                switch (messageType)
                {
                    case "OnBetPlacement":
                        var onBetPlacement = JsonSerializer.Deserialize<OnClientTopMarketBetPlacement>(payload);
                        await _bettingRepository.ProcessBetPlacement(onBetPlacement);

                        _messageBroker.Acknowledge(e);
                        break;
                    case "OnBetSettlement":
                        var onBetSettlement = JsonSerializer.Deserialize<OnClientTopMarketBetSettlement>(payload);
                        await _bettingRepository.ProcessBetSettlement(onBetSettlement);

                        _messageBroker.Acknowledge(e);
                        break;
                    default:
                        _messageBroker.Acknowledge(e);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
            }
        }
    }
}
