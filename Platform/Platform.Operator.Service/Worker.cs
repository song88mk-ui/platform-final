using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Operator.Service.Repository;
using Platform.Operator.Service.Utility;
using Platform.Shared.MessageModels;
using RabbitMQ.Client.Events;

namespace Platform.Operator.Service
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
            _messageBroker.Connect();
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
                switch (messageType)
                {
                    case "OnBetPlacement":
                        var onBetPlacement = JsonSerializer.Deserialize<OnBetPlacement>(payload);

                        _bettingRepository.ProcesssBetPlacement(onBetPlacement, _messageBroker, correlationId);
                        break;

                    case "OnBetSettlement":
                        var onBetSettlement = JsonSerializer.Deserialize<OnBetSettlement>(payload);

                        _bettingRepository.ProcesssBetSettlement(onBetSettlement, _messageBroker, correlationId);
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
