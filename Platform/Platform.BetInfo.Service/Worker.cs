using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.BetInfo.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.BetInfo.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;

        public Worker(IMessageBroker messageBroker, IBettingRepository bettingRepository, ILogger<Worker> logger)
        {
            _messageBroker = messageBroker;
            _bettingRepository = bettingRepository;
            _logger = logger;
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
            _messageBroker.ConnectWithManualAck();
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }

        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;

            try
            {
                switch (messageType)
                {
                    case "OnBetPlacement":
                        var betPlacement = JsonSerializer.Deserialize<OnBetPlacement>(payload);
                        _bettingRepository.ProcessBetPlacement(betPlacement);
                        break;
                }
                _messageBroker.Acknowledge(e);
            }
            catch (Exception ex)
            {
                _logger.LogError($"error processing message: {ex.Message}, stacktrace: {ex.StackTrace}");
            }
        }
    }
}
