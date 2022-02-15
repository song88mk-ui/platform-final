using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text;
using Platform.Shared.Utility;
using Platform.Settlement.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Enums;

namespace Platform.Settlement.Service
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
                    case "OnBetSettlement":
                        var onBetSettlement = JsonSerializer.Deserialize<OnUOFBetSettlement>(payload);
                        _bettingRepository.ProcesssBetSettlement(onBetSettlement, _messageBroker, correlationId);
                        break;

                    case "OnBetCancellation":
                        var onBetCancellation = JsonSerializer.Deserialize<OnUOFBetCancellation>(payload);
                        _bettingRepository.ProcessBetCancellation(onBetCancellation, _messageBroker, correlationId);
                        break;

                    case "OnBetBackOfficeCancellation":
                        var onBOBetCancellation = JsonSerializer.Deserialize<OnBOBetCancellaton>(payload);
                        _bettingRepository.ProcessBackOfficeBetCancellation(onBOBetCancellation, _messageBroker, correlationId);
                        break;

                    case "OnBetBackOfficeManualSettlement":
                        var onBOBetManualSettlement = JsonSerializer.Deserialize<OnBOBetManualSettlement>(payload);
                        _bettingRepository.ProcesssBackOfficeManualSettlement(onBOBetManualSettlement, _messageBroker, correlationId);
                        break;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
